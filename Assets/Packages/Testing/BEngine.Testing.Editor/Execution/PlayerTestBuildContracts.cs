using BEngine.Build;

namespace BEngine.Testing.Editor;

/// <summary>Inputs owned by the test session; a framework chooses how to produce its executable and content.</summary>
public sealed record PlayerTestBuildRequest(string ProjectPath, string OutputDirectory,
    string ContentOutputDirectory, string? TargetId = null);

public sealed record PlayerTestBuildProgress(string Stage, string Message, float Progress);

/// <summary>Optional framework startup arguments, separate from the test runner's own protocol.</summary>
public sealed class PlayerTestLaunchOptions
{
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; init; } =
        new Dictionary<string, string>();
    public string ContentUrlEnvironmentVariable { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> BrowserQueryParameters { get; init; } =
        new Dictionary<string, string>();
    public IReadOnlyDictionary<string, bool> AndroidBooleanExtras { get; init; } =
        new Dictionary<string, bool>();
}

/// <summary>A runnable artifact description. It carries no framework-specific build settings or result types.</summary>
public sealed record PlayerTestBuild(BuildTargetDescriptor Target, string OutputDirectory,
    string ContentDirectory)
{
    public string ExecutablePath { get; init; } = string.Empty;
    public string AndroidApplicationIdentifier { get; init; } = "com.bengine.game";
    public PlayerTestLaunchOptions LaunchOptions { get; init; } = new();
}

/// <summary>Optional build workflow supplied by a host or framework. Edit/Play tests do not require it.</summary>
public interface IPlayerTestBuildProvider
{
    Task<PlayerTestBuild> BuildAsync(PlayerTestBuildRequest request,
        IProgress<PlayerTestBuildProgress>? progress = null, CancellationToken cancellationToken = default);
}
