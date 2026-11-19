namespace BEngine.TiledMap;

internal static class TilePalettePath
{
    internal static string Resolve(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var paths = RuntimeServices.GetRequiredService<global::BEngine.Platform.PathUtility>();
        var platform = RuntimeServices.GetRequiredService<global::BEngine.Platform.PlatformUtility>();
        if (paths.IsPathRooted(path)) return paths.GetFullPath(path);
        var normalized = path.Replace('/', paths.DirectorySeparatorChar);
        if (normalized.Equals("Assets", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith($"Assets{paths.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            var dataPath = paths.GetFullPath(Application.dataPath);
            var root = paths.GetFileName(dataPath).Equals("Assets", StringComparison.OrdinalIgnoreCase)
                ? paths.GetDirectoryName(dataPath) ?? platform.CurrentDirectory
                : platform.CurrentDirectory;
            return paths.GetFullPath(paths.Combine(root, normalized));
        }
        return paths.GetFullPath(paths.Combine(Application.dataPath, normalized));
    }
}
