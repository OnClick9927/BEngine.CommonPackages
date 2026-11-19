using System.Diagnostics;
using System.Text.Json;
using BEngine.Hosting;
using BEngine.Platform;

namespace BEngine.Testing.Editor;

public static class AndroidPlayerTestLauncher
{
    public static async Task<TestRunResult> Run(PlayerTestLaunchRequest request, CancellationToken cancellationToken)
    {
        var adb = Environment.GetEnvironmentVariable("BENGINE_TEST_ADB") ??
            BEngine.Editor.EditorServices.GetRequiredService<IEditorHostTools>().FindAndroidDebugBridge() ??
            throw new FileNotFoundException("Install Android platform-tools or configure BENGINE_TEST_ADB.");
        var processRunner = ProcessUtility.Current;
        var serial = Environment.GetEnvironmentVariable("BENGINE_TEST_ANDROID_SERIAL");
        var package = request.AndroidApplicationIdentifier;
        if (package.Length == 0 || Matches877())
            throw new ArgumentException("Invalid Android application identifier.");
        [global::BEngine.SkipLoopGuard]
        bool Matches877()
        {
            var matchSource877 = package;
            ArgumentNullException.ThrowIfNull(matchSource877, "source");
            global::System.ReadOnlySpan<char> matchValues877 = matchSource877.AsSpan();
            foreach (var character in matchValues877)
            {
                if (!char.IsAsciiLetterOrDigit(character) && character is not ('.' or '_'))
                    return true;
            }
        
            return false;
        }
        var apks = Directory.GetFiles(request.Build.OutputDirectory, "*.apk", SearchOption.AllDirectories).OrderByDescending(path => path.Contains("-Signed", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (apks.Length == 0) throw new FileNotFoundException("Android Player tests require an APK build.");
        var port = new Uri(request.ContentUrl).Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var remote = "files/bengine-test-" + Guid.NewGuid().ToString("N") + ".json";
        await Adb("install", "-r", apks[0]); await Adb("reverse", "tcp:" + port, "tcp:" + port);
        try
        {
            await Adb("shell", "am", "force-stop", package);
            var resolved = await AdbAllowFailure("shell", "cmd", "package", "resolve-activity", "--brief", package);
            var component = Scan2344(resolved.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)) ?? throw new InvalidOperationException("Android test launcher activity could not be resolved: " + resolved.Output);
            [global::BEngine.SkipLoopGuard]
            string? Scan2344(global::System.Collections.Generic.IEnumerable<string> scanSource2344)
            {
                ArgumentNullException.ThrowIfNull(scanSource2344, "source");
                if (scanSource2344 is global::System.Collections.Generic.IList<string> scanList2344)
                {
                    for (var scanIndex2344 = scanList2344.Count - 1; scanIndex2344 >= 0; scanIndex2344--)
                    {
                        var scanItem2344 = scanList2344[scanIndex2344];
                        if (Match2344(scanItem2344))
                            return scanItem2344;
                    }
            
                    return default!;
                }
            
                string? result = default!;
                foreach (var scanItem2344 in scanSource2344)
                    if (Match2344(scanItem2344))
                        result = scanItem2344;
                return result;
                bool Match2344(string line) => line.StartsWith(package + "/", StringComparison.Ordinal);
            }
            var environment = PlayerTestLaunchEnvironment.Create(request);
            environment.Remove("BENGINE_TEST_RUNNER_RESULT");
            var encoded = HostLaunchEnvironment.Encode(environment,
                new Dictionary<string, string> { ["BENGINE_TEST_RUNNER_RESULT"] = remote["files/".Length..] });
            var launch = new List<string> { "shell", "am", "start", "-n", component,
                "--es", HostLaunchEnvironment.ArgumentName, encoded };
            foreach (var extra in request.Build.LaunchOptions.AndroidBooleanExtras)
                launch.AddRange(["--ez", extra.Key, extra.Value ? "true" : "false"]);
            var started = await AdbAllowFailure(launch.ToArray());
            if (started.Code != 0 || started.Output.Contains("Error", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Android test Activity failed to start: " + started.Output);
            var launchedAt = Stopwatch.GetTimestamp();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await AdbAllowFailure("exec-out", "run-as", package, "cat", remote);
                if (result.Code == 0 && result.Output.TrimStart().StartsWith('{'))
                {
                    var parsed = JsonSerializer.Deserialize<TestRunResult>(result.Output) ?? throw new InvalidDataException("Android test result was empty.");
                    Directory.CreateDirectory(Path.GetDirectoryName(request.ResultPath)!); await File.WriteAllTextAsync(request.ResultPath, result.Output, cancellationToken); return parsed;
                }
                if (result.Output.Contains("not debuggable", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Android test APK must enable AndroidDebuggable to retrieve results.");
                if (Stopwatch.GetElapsedTime(launchedAt).TotalSeconds >= 15)
                {
                    var running = await AdbAllowFailure("shell", "pidof", package);
                    if (string.IsNullOrWhiteSpace(running.Output))
                    {
                        var crash = await AdbAllowFailure("logcat", "-d", "-t", "150", "AndroidRuntime:E", "BEngine:E", "*:S");
                        throw new InvalidOperationException("Android test Player exited before writing its result.\n" + crash.Output);
                    }
                }
                await Task.Delay(250, cancellationToken);
            }
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            foreach (var arguments in new[] { new[] { "shell", "am", "force-stop", package }, new[] { "reverse", "--remove", "tcp:" + port }, new[] { "shell", "run-as", package, "rm", "-f", remote } })
                try { await RunAdb(cleanup.Token, arguments); } catch (Exception) { /* Preserve the test result or original failure. */ }
        }
        async Task Adb(params string[] arguments)
        { var result = await AdbAllowFailure(arguments); if (result.Code != 0) throw new InvalidOperationException("adb: " + result.Output); }
        async Task<(int Code, string Output)> AdbAllowFailure(params string[] arguments)
            => await RunAdb(cancellationToken, arguments);
        async Task<(int Code, string Output)> RunAdb(CancellationToken operationToken, params string[] arguments)
        {
            var start = new ProcessRunRequest(adb);
            if (!string.IsNullOrWhiteSpace(serial)) { start.Arguments.Add("-s"); start.Arguments.Add(serial); }
            foreach (var argument in arguments) start.Arguments.Add(argument);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(operationToken); timeout.CancelAfter(TimeSpan.FromSeconds(60));
            var result = await processRunner.RunAsync(start, timeout.Token).ConfigureAwait(false);
            return (result.ExitCode, result.StandardOutput + result.StandardError);
        }
    }
}
