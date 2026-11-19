using BEngine.Platform;

namespace BEngine.TiledMap;

public sealed class FileTilemapTextureSource : ITilemapTextureSource
{
    private readonly BEngine.Rendering.TextureSourceReader _reader;
    private readonly PathUtility _paths;

    public static FileTilemapTextureSource Shared { get; } = new(
        RuntimeServices.GetRequiredService<global::BEngine.Platform.FileUtility>(),
        RuntimeServices.GetRequiredService<global::BEngine.Platform.PathUtility>(),
        RuntimeServices.GetRequiredService<global::BEngine.Platform.PlatformUtility>());

    public FileTilemapTextureSource(
        FileUtility fileSystem,
        PathUtility pathService,
        PlatformUtility platformService)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _paths = pathService ?? throw new ArgumentNullException(nameof(pathService));
        ArgumentNullException.ThrowIfNull(platformService);
        _reader = new(fileSystem, pathService, platformService);
    }

    public bool TryRead(string source, out byte[] bytes)
        => _reader.TryRead(source, out bytes);

    public string GetName(string source) => _paths.GetFileName(source);

}
