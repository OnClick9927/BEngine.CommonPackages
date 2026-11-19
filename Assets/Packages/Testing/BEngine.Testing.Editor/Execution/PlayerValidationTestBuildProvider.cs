namespace BEngine.Testing.Editor;

/// <summary>Consumes the optional editor build workflow without a dependency on its implementation.</summary>
internal sealed class PlayerValidationTestBuildProvider(IPlayerValidationBuildProvider provider) : IPlayerTestBuildProvider
{
    public async Task<PlayerTestBuild> BuildAsync(PlayerTestBuildRequest request,
        IProgress<PlayerTestBuildProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var bridge = progress is null ? null : new ValidationProgress(progress);
        var build = await provider.BuildAsync(new(request.ProjectPath, request.OutputDirectory,
            request.ContentOutputDirectory, request.TargetId), bridge, cancellationToken).ConfigureAwait(false);
        return DescribeBuild(build);
    }

    internal static PlayerTestBuild DescribeBuild(PlayerValidationBuild build)
    {
        ArgumentNullException.ThrowIfNull(build);
        return new(build.Target, build.OutputDirectory, build.ContentDirectory)
        {
            ExecutablePath = build.ExecutablePath,
            AndroidApplicationIdentifier = build.AndroidApplicationIdentifier,
            LaunchOptions = new()
            {
                EnvironmentVariables = build.LaunchOptions.EnvironmentVariables,
                ContentUrlEnvironmentVariable = build.LaunchOptions.ContentUrlEnvironmentVariable,
                BrowserQueryParameters = build.LaunchOptions.BrowserQueryParameters,
                AndroidBooleanExtras = build.LaunchOptions.AndroidBooleanExtras
            }
        };
    }

    private sealed class ValidationProgress(IProgress<PlayerTestBuildProgress> target) : IProgress<PlayerValidationBuildProgress>
    {
        public void Report(PlayerValidationBuildProgress value) => target.Report(new(value.Stage, value.Message, value.Progress));
    }
}
