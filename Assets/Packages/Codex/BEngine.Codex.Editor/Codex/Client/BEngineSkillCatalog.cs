using EditorCapabilities = BEngine.Editor.EditorServices;
using BEngine.Platform;
namespace BEngine.Editor.Codex;

internal static class BEngineSkillCatalog
{
    private const string SkillFileName = "SKILL.md";

    internal static IReadOnlyList<string> Discover(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        var fullProjectRoot = EditorCapabilities.GetRequiredService<PathUtility>().GetFullPath(projectRoot);
        var skills = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        if (EditorResource.FindPath($"Skills/bengine-core/{SkillFileName}") is { } coreSkill)
            skills.Add(EditorCapabilities.GetRequiredService<PathUtility>().GetFullPath(coreSkill));

        var packagesRoot = EditorCapabilities.GetRequiredService<PathUtility>().Combine(fullProjectRoot, "Library", "Packages");
        if (!EditorCapabilities.GetRequiredService<FileUtility>().DirectoryExists(packagesRoot)) return skills.ToArray();

        foreach (var packageRoot in EditorCapabilities.GetRequiredService<FileUtility>().EnumerateDirectories(packagesRoot, "*", SearchOption.TopDirectoryOnly))
        {
            foreach (var skillsRoot in new[]
                     {
                         EditorCapabilities.GetRequiredService<PathUtility>().Combine(packageRoot, "Skills"),
                         EditorCapabilities.GetRequiredService<PathUtility>().Combine(packageRoot, EditorResource.DirectoryName, "Skills")
                     })
            {
                if (!EditorCapabilities.GetRequiredService<FileUtility>().DirectoryExists(skillsRoot)) continue;
                foreach (var skillFile in EditorCapabilities.GetRequiredService<FileUtility>().EnumerateFiles(skillsRoot, SkillFileName, SearchOption.AllDirectories))
                    skills.Add(EditorCapabilities.GetRequiredService<PathUtility>().GetRelativePath(fullProjectRoot, skillFile).Replace('\\', '/'));
            }
        }

        return skills.ToArray();
    }
}
