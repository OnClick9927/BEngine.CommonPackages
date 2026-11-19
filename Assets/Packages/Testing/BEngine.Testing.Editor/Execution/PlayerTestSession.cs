using EditorCapabilities = BEngine.Editor.EditorServices;
using BEngine.Platform;
using System.Text.Json;
using BEngine.Build;
using BEngine.DependencyInjection;

namespace BEngine.Testing.Editor;

public static class PlayerTestSession
{
    /// <summary>Runs a Player described by an optional editor build workflow.</summary>
    public static Task<TestRunResult> RunBuiltPlayerAsync(PlayerValidationBuild build, string contentDirectory, string resultPath,
        bool collectCoverage = false, string? androidApplicationIdentifier = null, IReadOnlyCollection<string>? testIds = null, CancellationToken cancellationToken = default) =>
        RunBuiltPlayerAsync(PlayerValidationTestBuildProvider.DescribeBuild(build), contentDirectory, resultPath,
            collectCoverage, androidApplicationIdentifier ?? build.AndroidApplicationIdentifier, testIds, cancellationToken);

    /// <summary>Runs a workflow-produced Player for an explicit project.</summary>
    public static Task<TestRunResult> RunBuiltPlayerAsync(PlayerValidationBuild build, string contentDirectory, string resultPath,
        string projectPath, bool collectCoverage = false, string? androidApplicationIdentifier = null, IReadOnlyCollection<string>? testIds = null, CancellationToken cancellationToken = default) =>
        RunBuiltPlayerAsync(PlayerValidationTestBuildProvider.DescribeBuild(build), contentDirectory, resultPath,
            projectPath, collectCoverage, androidApplicationIdentifier ?? build.AndroidApplicationIdentifier, testIds, cancellationToken);

    /// <summary>Runs an already-built device/browser Player against a prepared main content directory.</summary>
    public static Task<TestRunResult> RunBuiltPlayerAsync(PlayerTestBuild build, string contentDirectory, string resultPath,
        bool collectCoverage = false, string androidApplicationIdentifier = "com.bengine.game", IReadOnlyCollection<string>? testIds = null, CancellationToken cancellationToken = default) =>
        RunBuiltPlayerAsync(build, contentDirectory, resultPath, EditorApplication.projectPath,
            collectCoverage, androidApplicationIdentifier, testIds, cancellationToken);

    /// <summary>Runs an already-built Player for an explicit project, including when no editor project is open.</summary>
    public static async Task<TestRunResult> RunBuiltPlayerAsync(PlayerTestBuild build, string contentDirectory, string resultPath,
        string projectPath, bool collectCoverage = false, string androidApplicationIdentifier = "com.bengine.game", IReadOnlyCollection<string>? testIds = null, CancellationToken cancellationToken = default)
    {
        if (!PlayerTestLaunchers.TryGet(build.Target.TargetId, out var launcher)) throw new NotSupportedException("This target has no device/browser test launcher.");
        await using var server = LoopbackContentServer.Start(contentDirectory);
        var result = await launcher(new(build, server.BaseUrl, resultPath, collectCoverage, testIds)
            { AndroidApplicationIdentifier = androidApplicationIdentifier, ProjectPath = projectPath }, cancellationToken);
        if (result.Coverage is not null) CodeCoverage.Merge(result.Coverage);
        return result;
    }
    public static Task<TestRunResult> RunAsync(
        bool collectCoverage,
        IProgress<PlayerTestBuildProgress>? progress = null,
        CancellationToken cancellationToken = default) => RunProjectAsync(EditorApplication.projectPath, collectCoverage, progress: progress, cancellationToken: cancellationToken);
    public static async Task<TestRunResult> RunProjectAsync(string projectPath, bool collectCoverage,
        string? targetId = null, IReadOnlyCollection<string>? testIds = null,
        IProgress<PlayerTestBuildProgress>? progress = null, CancellationToken cancellationToken = default,
        IPlayerTestBuildProvider? buildProvider = null)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new InvalidOperationException("A project must be open to run Player tests.");
        cancellationToken.ThrowIfCancellationRequested();
        buildProvider ??= EditorCapabilities.GetService<IPlayerTestBuildProvider>() ??
            RuntimeServiceContributions.GetDefault<IPlayerTestBuildProvider>();
        if (buildProvider is null && (EditorCapabilities.GetService<IPlayerValidationBuildProvider>() ??
                RuntimeServiceContributions.GetDefault<IPlayerValidationBuildProvider>()) is { } workflow)
            buildProvider = new PlayerValidationTestBuildProvider(workflow);
        if (buildProvider is null)
            throw new InvalidOperationException("Player tests require a registered IPlayerTestBuildProvider. " +
                "Supply an IPlayerValidationBuildProvider workflow, a custom test build provider, or run Edit/Play tests without one.");
        var root = Path.Combine(projectPath, "Temp", "TestRunner");
        Directory.CreateDirectory(root);
        var output = Path.Combine(projectPath, "Build", "TestRunner", "player-" + Guid.NewGuid().ToString("N"));
        var contentOutput = Path.Combine(root, "content-" + Guid.NewGuid().ToString("N"));
        var resultPath = Path.Combine(root, "result-" + Guid.NewGuid().ToString("N") + ".json");
        var build = await buildProvider.BuildAsync(new(projectPath, output, contentOutput, targetId),
            progress, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        await using var server = LoopbackContentServer.Start(build.ContentDirectory);
        if (PlayerTestLaunchers.TryGet(build.Target.TargetId, out var launcher))
        {
            var deviceResult = await launcher(new(build, server.BaseUrl, resultPath, collectCoverage, testIds)
                { AndroidApplicationIdentifier = build.AndroidApplicationIdentifier, ProjectPath = projectPath }, cancellationToken).ConfigureAwait(false);
            if (deviceResult.Coverage is not null) CodeCoverage.Merge(deviceResult.Coverage);
            return deviceResult;
        }
        EnsureLocalPlatform(build.Target.Platform);
        var executable = build.ExecutablePath;
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
            throw new FileNotFoundException("The test build provider did not return an existing executable.", executable);
        var start = new ProcessRunRequest(executable)
        {
            WorkingDirectory = build.OutputDirectory,
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };
        var environment = PlayerTestLaunchEnvironment.Create(new(build, server.BaseUrl, resultPath, collectCoverage, testIds));
        environment["BENGINE_TEST_RUNNER_RESULT"] = resultPath;
        foreach (var pair in environment) start.EnvironmentVariables[pair.Key] = pair.Value;
        var process = await ProcessUtility.Current.RunAsync(start, cancellationToken).ConfigureAwait(false);
        if (!File.Exists(resultPath))
            throw new InvalidDataException(
                $"Player exited with code {process.ExitCode} without writing a test result: {resultPath}");
        var result = JsonSerializer.Deserialize<TestRunResult>(await File.ReadAllTextAsync(resultPath, cancellationToken)
                         .ConfigureAwait(false)) ??
                     throw new InvalidDataException("Player test result JSON was empty.");
        if (process.ExitCode != 0 && result.Succeeded)
            throw new InvalidDataException($"Player reported successful tests but exited with code {process.ExitCode}.");
        if (result.Coverage is not null) CodeCoverage.Merge(result.Coverage);
        return result;
    }

    private sealed class LoopbackContentServer : IAsyncDisposable
    {
        private readonly string _root;
        private readonly PlayerTestHttpServer _server;

        private LoopbackContentServer(string root)
        {
            _root = Path.GetFullPath(root);
            if (!Directory.Exists(_root))
                throw new DirectoryNotFoundException("Player test content root was not built: " + _root);
            _server = new PlayerTestHttpServer(HandleAsync);
        }

        internal string BaseUrl => _server.BaseUrl;
        internal static LoopbackContentServer Start(string root) => new(root);

        private async Task HandleAsync(IEditorHttpContext context, CancellationToken cancellationToken)
        {
            context.SetResponseHeader("Access-Control-Allow-Origin", "*");
            context.SetResponseHeader("Access-Control-Allow-Methods", "GET, OPTIONS");
            context.SetResponseHeader("Access-Control-Allow-Headers", "*");
            context.SetResponseHeader("Cross-Origin-Resource-Policy", "cross-origin");
            if (context.Method == "OPTIONS")
            {
                context.StatusCode = 204;
                context.ResponseContentLength = 0;
                return;
            }
            if (context.Method != "GET") { context.StatusCode = 405; context.ResponseContentLength = 0; return; }
            var relative = Uri.UnescapeDataString(context.Url.AbsolutePath).TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var path = Path.GetFullPath(Path.Combine(_root, relative));
            if (!path.StartsWith(_root + Path.DirectorySeparatorChar, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) ||
                !File.Exists(path))
            { context.StatusCode = 404; context.ResponseContentLength = 0; return; }
            await using var file = File.OpenRead(path);
            context.StatusCode = 200;
            context.ResponseContentLength = file.Length;
            await file.CopyToAsync(context.ResponseBody, cancellationToken).ConfigureAwait(false);
        }

        public ValueTask DisposeAsync() => _server.DisposeAsync();
    }

    private static void EnsureLocalPlatform(BuildTargetPlatform platform)
    {
        if (!(platform == BuildTargetPlatform.Windows && OperatingSystem.IsWindows() ||
              platform == BuildTargetPlatform.Linux && OperatingSystem.IsLinux() ||
              platform == BuildTargetPlatform.MacOS && OperatingSystem.IsMacOS()))
            throw new PlatformNotSupportedException($"No local launcher for '{platform}' on this host. Use PlayerTestLaunchers.Register for a device/browser launcher.");
    }
}
