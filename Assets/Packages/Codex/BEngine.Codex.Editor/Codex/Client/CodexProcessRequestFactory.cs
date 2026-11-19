using EditorCapabilities = BEngine.Editor.EditorServices;
using BEngine.Platform;

namespace BEngine.Editor.Codex;

internal static class CodexProcessRequestFactory
{
    internal static string ResolveHome() =>
        EditorCapabilities.GetRequiredService<PathUtility>().Combine(EditorDataPaths.rootPath, "Preferences", "Codex");

    internal static ProcessHostRequest Create(string executable, string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        var extension = EditorCapabilities.GetRequiredService<PathUtility>().GetExtension(executable);
        if (EditorCapabilities.GetRequiredService<PlatformUtility>().IsWindows && extension is ".cmd" or ".bat")
        {
            var commandInterpreter = EditorCapabilities.GetRequiredService<PlatformUtility>().GetEnvironmentVariable("ComSpec");
            if (string.IsNullOrWhiteSpace(commandInterpreter))
                commandInterpreter = EditorCapabilities.GetRequiredService<PathUtility>().Combine(
                    EditorCapabilities.GetRequiredService<PlatformUtility>().GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
            return CreateBase(commandInterpreter, workingDirectory,
                $"/d /s /c \"\"{executable}\" app-server\"");
        }

        var direct = CreateBase(executable, workingDirectory);
        direct.Arguments.Add("app-server");
        return direct;
    }

    private static ProcessHostRequest CreateBase(
        string executable,
        string workingDirectory,
        string? rawArguments = null)
    {
        var request = new ProcessHostRequest(executable)
        {
            WorkingDirectory = workingDirectory,
            RawArguments = rawArguments,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        var searchPath = CodexExecutableResolver.BuildEffectiveSearchPath();
        request.EnvironmentVariables["CODEX_HOME"] = ResolveHome();
        if (searchPath.Length > 0) request.EnvironmentVariables["PATH"] = searchPath;
        return request;
    }
}
