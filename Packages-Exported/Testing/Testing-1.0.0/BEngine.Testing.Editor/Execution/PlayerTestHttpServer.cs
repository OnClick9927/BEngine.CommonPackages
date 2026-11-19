using BEngine.Platform;

namespace BEngine.Testing.Editor;

/// <summary>Lifetime of test-fixture requests; native listening belongs to the injected platform.</summary>
internal sealed class PlayerTestHttpServer : IAsyncDisposable
{
    private readonly IEditorHttpListener _listener;
    private readonly CancellationTokenSource _stop = new();
    private readonly Func<IEditorHttpContext, CancellationToken, Task> _handle;
    private readonly Action<Exception>? _onFailure;
    private readonly HashSet<IEditorHttpContext> _active = [];
    private readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task _loop;
    private bool _closing;

    internal PlayerTestHttpServer(Func<IEditorHttpContext, CancellationToken, Task> handle, Action<Exception>? onFailure = null)
    {
        _handle = handle;
        _onFailure = onFailure;
        _listener = BEngine.Editor.EditorServices.GetRequiredService<IEditorHttpListenerFactory>().ListenLoopback();
        _loop = ServeAsync();
    }

    internal string BaseUrl => _listener.BaseAddress.AbsoluteUri;

    private async Task ServeAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                var context = await _listener.AcceptAsync(_stop.Token).ConfigureAwait(false);
                lock (_active)
                {
                    if (_closing) { context.Dispose(); return; }
                    _active.Add(context);
                }
                _ = HandleAsync(context);
            }
        }
        catch (Exception exception)
        {
            if (!_stop.IsCancellationRequested) _onFailure?.Invoke(exception);
        }
    }

    private async Task HandleAsync(IEditorHttpContext context)
    {
        try { await _handle(context, _stop.Token).ConfigureAwait(false); }
        catch (Exception exception)
        {
            if (!_stop.IsCancellationRequested) _onFailure?.Invoke(exception);
        }
        finally
        {
            try { context.Dispose(); }
            finally
            {
                lock (_active)
                {
                    _active.Remove(context);
                    if (_closing && _active.Count == 0) _drained.TrySetResult();
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (_active) _closing = true;
        _stop.Cancel();
        _listener.Dispose();
        await _loop.ConfigureAwait(false);
        IEditorHttpContext[] contexts;
        lock (_active)
        {
            contexts = _active.ToArray();
            if (contexts.Length == 0) _drained.TrySetResult();
        }
        foreach (var context in contexts) context.Dispose();
        await _drained.Task.ConfigureAwait(false);
        _stop.Dispose();
    }
}
