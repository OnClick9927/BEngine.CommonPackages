namespace BEngine.Reactive;

internal sealed class AnonymousObserver<T>(
    Action<T>? onNext,
    Action<Exception>? onErrorResume,
    Action<BResult>? onCompleted) : BObserver<T>
{
    protected override void OnNextCore(T value) => onNext?.Invoke(value);

    protected override void OnErrorResumeCore(Exception error)
    {
        var handler = onErrorResume;
        if (handler is null) base.OnErrorResumeCore(error);
        else handler(error);
    }

    protected override void OnCompletedCore(BResult result)
    {
        try
        {
            var handler = onCompleted;
            if (handler is null) base.OnCompletedCore(result);
            else handler(result);
        }
        finally { ClearCallbacks(); }
    }

    protected override void DisposeCore() => ClearCallbacks();

    // The returned subscription can outlive its UI/view. A stopped observer cannot invoke
    // these callbacks again, so retaining its token need not retain their captured objects.
    private void ClearCallbacks()
    {
        onNext = null;
        onErrorResume = null;
        onCompleted = null;
    }
}
