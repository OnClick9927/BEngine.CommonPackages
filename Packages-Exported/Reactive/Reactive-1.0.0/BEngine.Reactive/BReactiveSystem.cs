namespace BEngine.Reactive;

public static class BReactiveSystem
{
    private static Action<Exception> _unhandledException = DefaultUnhandledException;

    public static void RegisterUnhandledExceptionHandler(Action<Exception> handler) =>
        _unhandledException = handler ?? throw new ArgumentNullException(nameof(handler));

    public static Action<Exception> GetUnhandledExceptionHandler() => _unhandledException;

    internal static void ReportUnhandledException(Exception exception)
    {
        try { _unhandledException(exception); }
        catch { RuntimeServices.GetRequiredService<global::BEngine.Platform.PlatformUtility>().WriteDiagnostic(exception.ToString()); }
    }

    private static void DefaultUnhandledException(Exception exception) =>
        RuntimeServices.GetRequiredService<global::BEngine.Platform.PlatformUtility>().WriteDiagnostic($"Unhandled reactive exception: {exception}");
}
