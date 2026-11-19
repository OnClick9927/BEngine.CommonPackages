using EditorCapabilities = BEngine.Editor.EditorServices;
using BEngine.Platform;
using BEngine.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace BEngine.Editor.Codex;

public sealed class CodexProjectStore
{
    private readonly string _legacySettingsPath;

    public string ProjectRoot { get; }
    public string SettingsPath { get; }
    public string SessionPath { get; }

    public CodexProjectStore(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ProjectRoot = EditorCapabilities.GetRequiredService<PathUtility>().GetFullPath(projectRoot);
        SettingsPath = EditorCapabilities.GetRequiredService<PathUtility>().Combine(EditorDataPaths.preferencesDirectoryPath, "CodexSettings.yaml");
        _legacySettingsPath = EditorCapabilities.GetRequiredService<PathUtility>().Combine(ProjectRoot, "ProjectSettings", "CodexSettings.yaml");
        SessionPath = EditorCapabilities.GetRequiredService<PathUtility>().Combine(ProjectRoot, "Library", "Codex", "Session.yaml");
    }

    public CodexProjectSettingsData LoadSettings()
    {
        MigrateLegacySettings();
        if (!EditorCapabilities.GetRequiredService<FileUtility>().FileExists(SettingsPath))
        {
            var defaults = new CodexProjectSettingsData();
            SaveSettings(defaults);
            return defaults;
        }

        var settings = YamlUtility.Load<CodexProjectSettingsData>(SettingsPath);
        Validate(settings.Format, settings.Version, "BEngine.CodexSettings", SettingsPath);
        if (CodexProtocolSettings.Normalize(settings)) SaveSettings(settings);
        return settings;
    }

    public CodexSessionDocument LoadSession()
    {
        if (!EditorCapabilities.GetRequiredService<FileUtility>().FileExists(SessionPath)) return new CodexSessionDocument();
        var session = YamlUtility.Load<CodexSessionDocument>(SessionPath);
        Validate(session.Format, session.Version, "BEngine.CodexSession", SessionPath);
        session.Transcript ??= [];
        return session;
    }

    public void SaveSettings(CodexProjectSettingsData settings)
    {
        MigrateLegacySettings();
        CodexProtocolSettings.Normalize(settings);
        settings.Save(SettingsPath);
    }

    public void SaveSession(CodexSessionDocument session)
    {
        const int maximumEntries = 250;
        if (session.Transcript.Count > maximumEntries)
        {
            session.Transcript = session.Transcript.Skip(session.Transcript.Count - maximumEntries).ToList();
        }
        session.Save(SessionPath);
    }

    public string NormalizeContextPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = EditorCapabilities.GetRequiredService<PathUtility>().IsPathRooted(path)
            ? EditorCapabilities.GetRequiredService<PathUtility>().GetFullPath(path)
            : EditorCapabilities.GetRequiredService<PathUtility>().GetFullPath(EditorCapabilities.GetRequiredService<PathUtility>().Combine(ProjectRoot, path.Replace('/', EditorCapabilities.GetRequiredService<PathUtility>().DirectorySeparatorChar)));
        var rootWithSeparator = ProjectRoot.EndsWith(EditorCapabilities.GetRequiredService<PathUtility>().DirectorySeparatorChar)
            ? ProjectRoot
            : ProjectRoot + EditorCapabilities.GetRequiredService<PathUtility>().DirectorySeparatorChar;
        if (!fullPath.Equals(ProjectRoot, StringComparison.OrdinalIgnoreCase) &&
            !fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Codex context path escapes the project: {path}");
        }
        if (!EditorCapabilities.GetRequiredService<FileUtility>().FileExists(fullPath) && !EditorCapabilities.GetRequiredService<FileUtility>().DirectoryExists(fullPath))
        {
            throw new FileNotFoundException("Codex context path does not exist.", fullPath);
        }
        return EditorCapabilities.GetRequiredService<PathUtility>().GetRelativePath(ProjectRoot, fullPath).Replace('\\', '/');
    }

    private static void Validate(string format, int version, string expectedFormat, string path)
    {
        if (format != expectedFormat || version != 1)
        {
            throw new InvalidDataException($"Unsupported YAML document '{format}' v{version}: {path}");
        }
    }

    private void MigrateLegacySettings()
    {
        MigrateLegacyGlobalSettings();
        if (!EditorCapabilities.GetRequiredService<FileUtility>().FileExists(_legacySettingsPath)) return;
        EditorCapabilities.GetRequiredService<FileUtility>().CreateDirectory(EditorCapabilities.GetRequiredService<PathUtility>().GetDirectoryName(SettingsPath)!);
        var destination = EditorCapabilities.GetRequiredService<FileUtility>().FileExists(SettingsPath)
            ? CreateConflictBackupPath()
            : SettingsPath;
        if (!EditorCapabilities.GetRequiredService<FileUtility>().FileExists(destination)) EditorCapabilities.GetRequiredService<FileUtility>().CopyFile(_legacySettingsPath, destination, overwrite: false);
        else if (!FilesHaveSameContents(_legacySettingsPath, destination))
            throw new IOException($"Codex settings migration destination already exists: {destination}");
        try { EditorCapabilities.GetRequiredService<FileUtility>().DeleteFile(_legacySettingsPath); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private void MigrateLegacyGlobalSettings()
    {
        var source = EditorCapabilities.GetRequiredService<PathUtility>().Combine(EditorCapabilities.GetRequiredService<PlatformUtility>().GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BEngine", "CodexSettings.yaml");
        if (!EditorCapabilities.GetRequiredService<FileUtility>().FileExists(source)) return;
        EditorCapabilities.GetRequiredService<FileUtility>().CreateDirectory(EditorCapabilities.GetRequiredService<PathUtility>().GetDirectoryName(SettingsPath)!);
        var destination = EditorCapabilities.GetRequiredService<FileUtility>().FileExists(SettingsPath) ? CreateConflictBackupPath() : SettingsPath;
        if (!EditorCapabilities.GetRequiredService<FileUtility>().FileExists(destination)) EditorCapabilities.GetRequiredService<FileUtility>().CopyFile(source, destination, false);
        if (FilesHaveSameContents(source, destination)) { try { EditorCapabilities.GetRequiredService<FileUtility>().DeleteFile(source); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    private string CreateConflictBackupPath()
    {
        var directory = EditorCapabilities.GetRequiredService<PathUtility>().Combine(EditorCapabilities.GetRequiredService<PathUtility>().GetDirectoryName(SettingsPath)!, "MigratedLegacy");
        EditorCapabilities.GetRequiredService<FileUtility>().CreateDirectory(directory);
        var projectName = SanitizeFileName(EditorCapabilities.GetRequiredService<PathUtility>().GetFileName(
            EditorCapabilities.GetRequiredService<PathUtility>().TrimEndingDirectorySeparator(ProjectRoot)));
        var normalizedRoot = EditorCapabilities.GetRequiredService<PathUtility>().GetFullPath(ProjectRoot).TrimEnd(EditorCapabilities.GetRequiredService<PathUtility>().DirectorySeparatorChar)
            .ToUpperInvariant();
        var projectHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedRoot)).AsSpan(0, 6));
        var prefix = $"CodexSettings-{projectName}-{projectHash}";
        var candidate = EditorCapabilities.GetRequiredService<PathUtility>().Combine(directory, prefix + ".yaml");
        for (var suffix = 2; EditorCapabilities.GetRequiredService<FileUtility>().FileExists(candidate) &&
                             !FilesHaveSameContents(_legacySettingsPath, candidate); suffix++)
            candidate = EditorCapabilities.GetRequiredService<PathUtility>().Combine(directory, $"{prefix}-{suffix}.yaml");
        return candidate;
    }

    private static bool FilesHaveSameContents(string left, string right)
    {
        return EditorCapabilities.GetRequiredService<FileUtility>().GetFileLength(left) ==
               EditorCapabilities.GetRequiredService<FileUtility>().GetFileLength(right) &&
               EditorCapabilities.GetRequiredService<FileUtility>().ReadAllBytes(left).AsSpan().SequenceEqual(EditorCapabilities.GetRequiredService<FileUtility>().ReadAllBytes(right));
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = EditorCapabilities.GetRequiredService<PathUtility>().GetInvalidFileNameChars().ToHashSet();
        var sanitized = new string (Gather7413(name)).Trim(' ', '.');
        [global::BEngine.SkipLoopGuard]
        char[] Gather7413(global::System.Collections.Generic.IEnumerable<char> items7413)
        {
            ArgumentNullException.ThrowIfNull(items7413, "source");
            char Evaluate7413(char character) => invalid.Contains(character) ? '_' : character;
            global::System.ReadOnlySpan<char> values;
            if (items7413.GetType() == typeof(char[]))
                values = (char[])items7413;
            else if (items7413.GetType() == typeof(global::System.Collections.Generic.List<char>))
                values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<char>)items7413);
            else if (items7413 is string text7413)
                values = text7413.AsSpan();
            else
            {
                return global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Select(items7413, Evaluate7413));
            }

            if (values.IsEmpty)
                return global::System.Array.Empty<char>();
            var result = new char[values.Length];
            for (var index = 0; index < values.Length; index++)
                result[index] = Evaluate7413(values[index]);
            return result;
        }
        return string.IsNullOrWhiteSpace(sanitized) ? "Project" : sanitized;
    }
}
