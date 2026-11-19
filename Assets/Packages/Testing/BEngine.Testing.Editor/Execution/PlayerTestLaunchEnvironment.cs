using System.Text.Json;

namespace BEngine.Testing.Editor;

/// <summary>Test protocol values layered over the content provider's portable startup inputs.</summary>
internal static class PlayerTestLaunchEnvironment
{
    internal static Dictionary<string, string> Create(PlayerTestLaunchRequest request)
    {
        var environment = new Dictionary<string, string>(request.Build.LaunchOptions.EnvironmentVariables, StringComparer.Ordinal)
        {
            ["BENGINE_TEST_RUNNER_MODE"] = "player",
            ["BENGINE_TEST_RUNNER_COVERAGE"] = request.CollectCoverage ? "1" : "0",
            ["BENGINE_TEST_RUNNER_IDS"] = request.TestIds is null ? string.Empty : JsonSerializer.Serialize(request.TestIds),
            ["BENGINE_TEST_RUNNER_RESULT"] = string.Empty,
            ["BENGINE_TEST_RUNNER_RESULT_URL"] = string.Empty
        };
        if (request.Build.LaunchOptions.ContentUrlEnvironmentVariable.Length > 0)
            environment[request.Build.LaunchOptions.ContentUrlEnvironmentVariable] = request.ContentUrl;
        return environment;
    }
}
