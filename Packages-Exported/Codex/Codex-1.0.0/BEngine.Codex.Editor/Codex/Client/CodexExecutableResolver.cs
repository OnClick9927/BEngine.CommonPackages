using EditorCapabilities = BEngine.Editor.EditorServices;
using BEngine.Platform;
namespace BEngine.Editor.Codex;

internal static class CodexExecutableResolver
{
    private static readonly string[] WindowsExecutableNames =
        ["codex.exe", "codex.com", "codex.cmd", "codex.bat", "codex"];

    internal static string Resolve(string? configuredPath, string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);

        if (TryResolveConfigured(configuredPath, projectRoot, out var executable)) return executable;

        var environmentPath = EditorCapabilities.GetRequiredService<PlatformUtility>().GetEnvironmentVariable("BENGINE_CODEX_PATH");
        if (TryResolveConfigured(environmentPath, projectRoot, out executable)) return executable;

        if (TryResolveFromSearchPath(out executable)) return executable;
        if (EditorCapabilities.GetRequiredService<PlatformUtility>().IsWindows && TryResolveDesktopInstallation(out executable)) return executable;

        throw new FileNotFoundException(
            "The Codex CLI was not found. Install Codex or set its full path in Codex Settings " +
            "(or BENGINE_CODEX_PATH).",
            "codex");
    }

    internal static string BuildEffectiveSearchPath() =>
        string.Join(EditorCapabilities.GetRequiredService<PathUtility>().PathSeparator, EnumerateSearchDirectories());

    private static bool TryResolveConfigured(string? value, string projectRoot, out string executable)
    {
        executable = string.Empty;
        var configured = Normalize(value);
        if (configured.Length == 0) return false;

        if (EditorCapabilities.GetRequiredService<PathUtility>().IsPathRooted(configured) || ContainsDirectorySeparator(configured))
        {
            var path = EditorCapabilities.GetRequiredService<PathUtility>().IsPathRooted(configured)
                ? EditorCapabilities.GetRequiredService<PathUtility>().GetFullPath(configured)
                : EditorCapabilities.GetRequiredService<PathUtility>().GetFullPath(EditorCapabilities.GetRequiredService<PathUtility>().Combine(projectRoot, configured));
            if (TryResolvePathOrDirectory(path, out executable)) return true;
            throw new FileNotFoundException("The configured Codex CLI path does not exist.", path);
        }

        if (TryResolveCommandName(configured, out executable)) return true;
        throw new FileNotFoundException($"The configured Codex command '{configured}' was not found on PATH.", configured);
    }

    private static bool TryResolveFromSearchPath(out string executable) =>
        TryResolveCommandName("codex", out executable);

    private static bool TryResolveCommandName(string commandName, out string executable)
    {
        executable = string.Empty;
        foreach (var directory in EnumerateSearchDirectories())
        {
            if (IsRestrictedPackagedApplicationDirectory(directory)) continue;
            foreach (var fileName in CandidateNames(commandName))
            {
                string candidate;
                try
                {
                    candidate = EditorCapabilities.GetRequiredService<PathUtility>().GetFullPath(EditorCapabilities.GetRequiredService<PathUtility>().Combine(directory, fileName));
                }
                catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
                {
                    continue;
                }
                if (!EditorCapabilities.GetRequiredService<FileUtility>().FileExists(candidate)) continue;
                executable = candidate;
                return true;
            }
        }
        return false;
    }

    private static bool TryResolveDesktopInstallation(out string executable)
    {
        executable = string.Empty;
        var localAppData = EditorCapabilities.GetRequiredService<PlatformUtility>().GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (localAppData.Length == 0) return false;
        var binRoot = EditorCapabilities.GetRequiredService<PathUtility>().Combine(localAppData, "OpenAI", "Codex", "bin");
        if (!EditorCapabilities.GetRequiredService<FileUtility>().DirectoryExists(binRoot)) return false;

        try
        {
            var candidate = EditorCapabilities.GetRequiredService<FileUtility>().EnumerateDirectories(binRoot, "*", SearchOption.TopDirectoryOnly)
                .Select(directory => EditorCapabilities.GetRequiredService<PathUtility>().Combine(directory, "codex.exe"))
                .Where(EditorCapabilities.GetRequiredService<FileUtility>().FileExists)
                .Select(path => new
                {
                    Path = path,
                    DirectoryStamp = EditorCapabilities.GetRequiredService<FileUtility>().GetLastWriteTimeUtc(
                        EditorCapabilities.GetRequiredService<PathUtility>().GetDirectoryName(path)!),
                    FileStamp = EditorCapabilities.GetRequiredService<FileUtility>().GetLastWriteTimeUtc(path)
                })
                .OrderByDescending(file => file.DirectoryStamp)
                .ThenByDescending(file => file.FileStamp)
                .Select(file => file.Path)
                .FirstOrDefault();
            if (candidate is null) return false;
            executable = candidate;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool TryResolvePathOrDirectory(string path, out string executable)
    {
        executable = string.Empty;
        if (EditorCapabilities.GetRequiredService<FileUtility>().FileExists(path))
        {
            executable = path;
            return true;
        }
        if (!EditorCapabilities.GetRequiredService<FileUtility>().DirectoryExists(path)) return TryResolveWithExtensions(path, out executable);
        foreach (var fileName in CandidateNames("codex"))
        {
            var candidate = EditorCapabilities.GetRequiredService<PathUtility>().Combine(path, fileName);
            if (!EditorCapabilities.GetRequiredService<FileUtility>().FileExists(candidate)) continue;
            executable = EditorCapabilities.GetRequiredService<PathUtility>().GetFullPath(candidate);
            return true;
        }
        return false;
    }

    private static bool TryResolveWithExtensions(string path, out string executable)
    {
        executable = string.Empty;
        if (!EditorCapabilities.GetRequiredService<PlatformUtility>().IsWindows || EditorCapabilities.GetRequiredService<PathUtility>().HasExtension(path)) return false;
        foreach (var extension in new[] { ".exe", ".com", ".cmd", ".bat" })
        {
            var candidate = path + extension;
            if (!EditorCapabilities.GetRequiredService<FileUtility>().FileExists(candidate)) continue;
            executable = EditorCapabilities.GetRequiredService<PathUtility>().GetFullPath(candidate);
            return true;
        }
        return false;
    }

    private static IEnumerable<string> EnumerateSearchDirectories()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in SearchPathValues())
        {
            foreach (var segment in value.Split(EditorCapabilities.GetRequiredService<PathUtility>().PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var directory = Normalize(segment);
                if (directory.Length == 0) continue;
                try { directory = EditorCapabilities.GetRequiredService<PathUtility>().GetFullPath(directory); }
                catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
                {
                    continue;
                }
                if (seen.Add(directory)) yield return directory;
            }
        }
    }

    private static IEnumerable<string> SearchPathValues()
    {
        yield return EditorCapabilities.GetRequiredService<PlatformUtility>().GetEnvironmentVariable("PATH") ?? string.Empty;
        if (!EditorCapabilities.GetRequiredService<PlatformUtility>().IsWindows) yield break;

        string? userPath = null;
        string? machinePath = null;
        try
        {
            userPath = EditorCapabilities.GetRequiredService<PlatformUtility>().GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
            machinePath = EditorCapabilities.GetRequiredService<PlatformUtility>().GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
        }
        catch (System.Security.SecurityException)
        {
        }
        yield return userPath ?? string.Empty;
        yield return machinePath ?? string.Empty;
    }

    private static IEnumerable<string> CandidateNames(string commandName)
    {
        if (!EditorCapabilities.GetRequiredService<PlatformUtility>().IsWindows || EditorCapabilities.GetRequiredService<PathUtility>().HasExtension(commandName))
        {
            yield return commandName;
            yield break;
        }
        foreach (var name in WindowsExecutableNames)
            yield return commandName.Equals("codex", StringComparison.OrdinalIgnoreCase)
                ? name
                : commandName + EditorCapabilities.GetRequiredService<PathUtility>().GetExtension(name);
    }

    private static bool IsRestrictedPackagedApplicationDirectory(string directory) =>
        EditorCapabilities.GetRequiredService<PlatformUtility>().IsWindows &&
        directory.Contains($"{EditorCapabilities.GetRequiredService<PathUtility>().DirectorySeparatorChar}Program Files{EditorCapabilities.GetRequiredService<PathUtility>().DirectorySeparatorChar}WindowsApps{EditorCapabilities.GetRequiredService<PathUtility>().DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase);

    private static bool ContainsDirectorySeparator(string value) =>
        value.Contains(EditorCapabilities.GetRequiredService<PathUtility>().DirectorySeparatorChar) || value.Contains(EditorCapabilities.GetRequiredService<PathUtility>().AltDirectorySeparatorChar);

    private static string Normalize(string? value) =>
        EditorCapabilities.GetRequiredService<PlatformUtility>().ExpandEnvironmentVariables(value?.Trim().Trim('"') ?? string.Empty);
}
