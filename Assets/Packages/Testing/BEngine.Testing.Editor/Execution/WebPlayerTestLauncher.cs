using System.Text;
using System.Text.Json;
using BEngine.Hosting;
using BEngine.Platform;

namespace BEngine.Testing.Editor;

public static class WebPlayerTestLauncher
{
    public static async Task<TestRunResult> Run(PlayerTestLaunchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var projectPath = string.IsNullOrWhiteSpace(request.ProjectPath)
            ? BEngine.Editor.EditorApplication.projectPath : request.ProjectPath;
        if (string.IsNullOrWhiteSpace(projectPath) || !Path.IsPathFullyQualified(projectPath) || !Directory.Exists(projectPath))
            throw new InvalidOperationException("An existing project path is required to run Web Player tests. Open a project or provide ProjectPath.");
        var profileRoot = Path.Combine(Path.GetFullPath(projectPath), "Temp", "TestRunner", "BrowserProfiles");
        var executable = Environment.GetEnvironmentVariable("BENGINE_TEST_BROWSER") ??
            BEngine.Editor.EditorServices.GetRequiredService<IEditorHostTools>().FindChromiumBrowser() ??
            throw new FileNotFoundException("Install Chrome/Edge/Chromium or configure BENGINE_TEST_BROWSER.");
        var index = Directory.GetFiles(request.Build.OutputDirectory, "index.html", SearchOption.AllDirectories).OrderBy(path => path.Length).FirstOrDefault()
            ?? throw new FileNotFoundException("Web build has no index.html.");
        var root = Path.GetDirectoryName(index)!; var token = Guid.NewGuid().ToString("N");
        var result = new TaskCompletionSource<TestRunResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await using var server = new PlayerTestHttpServer(Handle, exception => result.TrySetException(exception));
        var address = server.BaseUrl;
        var profile = Path.Combine(profileRoot, "bengine-browser-tests-" + token); Directory.CreateDirectory(profile);
        var environment = PlayerTestLaunchEnvironment.Create(request);
        environment["BENGINE_TEST_RUNNER_RESULT_URL"] = address + "__bengine_tests/" + token;
        var query = "?" + HostLaunchEnvironment.ArgumentName + "=" + HostLaunchEnvironment.Encode(environment);
        foreach (var parameter in request.Build.LaunchOptions.BrowserQueryParameters)
            query += "&" + Uri.EscapeDataString(parameter.Key) + "=" + Uri.EscapeDataString(parameter.Value);
        var start = new ProcessRunRequest(executable);
        // Edge's compatibility relaunch otherwise returns success before the actual browser process starts.
        if (Path.GetFileName(executable).Equals("msedge.exe", StringComparison.OrdinalIgnoreCase)) start.Arguments.Add("--edge-skip-compat-layer-relaunch");
        // The owned test browser must keep rendering even while the user's normal browser is foreground.
        foreach (var argument in new[] { "--headless=new", "--no-first-run", "--no-default-browser-check", "--disable-extensions", "--disable-background-timer-throttling", "--disable-backgrounding-occluded-windows", "--disable-renderer-backgrounding", "--enable-unsafe-swiftshader", "--user-data-dir=" + profile, address + query }) start.Arguments.Add(argument);
        Task<ProcessRunResult>? process = null;
        try
        {
            process = ProcessUtility.Current.RunAsync(start, stop.Token);
            var completed = await Task.WhenAny(result.Task, process).WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (completed != result.Task)
            {
                var exited = await process;
                var error = exited.StandardError;
                throw new InvalidOperationException($"Test browser exited with code {exited.ExitCode} without returning a result.\n" + error[..Math.Min(error.Length, 4096)]);
            }
            return await result.Task;
        }
        finally
        {
            stop.Cancel();
            if (process is not null)
            {
                try { await process.ConfigureAwait(false); }
                catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
                catch (Exception) when (process.IsFaulted) { /* Outcome was already observed above; still release the profile. */ }
            }
            var temporaryRoot = Path.GetFullPath(profileRoot).TrimEnd(Path.DirectorySeparatorChar);
            if (Path.GetDirectoryName(Path.GetFullPath(profile)) == temporaryRoot && Path.GetFileName(profile) == "bengine-browser-tests-" + token)
                for (var attempt = 0; attempt < 3 && Directory.Exists(profile); attempt++)
                {
                    try { Directory.Delete(profile, recursive: true); }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                    { await Task.Delay(200); }
                }
        }
        async Task Handle(IEditorHttpContext context, CancellationToken requestCancellation)
        {
                var relative = Uri.UnescapeDataString(context.Url.AbsolutePath).TrimStart('/');
                if (relative == "__bengine_tests/" + token && context.Method == "POST")
                {
                    if (context.RequestContentLength is <= 0 or > 16 * 1024 * 1024) { context.StatusCode = 413; context.ResponseContentLength = 0; return; }
                    using var reader = new StreamReader(context.RequestBody, Encoding.UTF8);
                    var json = await reader.ReadToEndAsync(requestCancellation); var parsed = JsonSerializer.Deserialize<TestRunResult>(json) ?? throw new InvalidDataException("Empty browser result.");
                    Directory.CreateDirectory(Path.GetDirectoryName(request.ResultPath)!); await File.WriteAllTextAsync(request.ResultPath, json, requestCancellation);
                    context.ResponseContentLength = 0;
                    result.TrySetResult(parsed); return;
                }
                if (context.Method != "GET") { context.StatusCode = 405; context.ResponseContentLength = 0; return; }
                var path = Path.GetFullPath(Path.Combine(root, relative.Length == 0 ? "index.html" : relative.Replace('/', Path.DirectorySeparatorChar)));
                if (!path.StartsWith(root + Path.DirectorySeparatorChar, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) || !File.Exists(path)) { context.StatusCode = 404; context.ResponseContentLength = 0; return; }
                context.ContentType = Path.GetExtension(path) switch { ".js" or ".mjs" => "text/javascript", ".wasm" => "application/wasm", ".html" => "text/html", ".json" => "application/json", _ => "application/octet-stream" };
                context.SetResponseHeader("Cross-Origin-Opener-Policy", "same-origin"); context.SetResponseHeader("Cross-Origin-Embedder-Policy", "require-corp");
                using var file = File.OpenRead(path); context.ResponseContentLength = file.Length; await file.CopyToAsync(context.ResponseBody, requestCancellation);
        }
    }
}
