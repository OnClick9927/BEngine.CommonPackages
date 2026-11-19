using BEngine.Rendering.Rhi;

namespace BEngine.TiledMap;

internal sealed class TilemapTextureCache : IDisposable
{
    private static readonly byte[] MissingPixels =
    [
        44, 46, 51, 255, 220, 62, 92, 255,
        220, 62, 92, 255, 44, 46, 51, 255
    ];

    private readonly IGraphicsDevice _device;
    private readonly ITilemapTextureSource _source;
    private readonly record struct Entry(IGraphicsTexture2D Texture, bool Opaque);
    private readonly Dictionary<string, Entry> _textures =
        new(StringComparer.Ordinal);
    private long _generation = -1;
    private IGraphicsTexture2D? _missing;

    internal TilemapTextureCache(
        IGraphicsDevice device,
        ITilemapTextureSource? source = null)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _source = source ?? TiledMapRuntimeServices.TextureSource;
    }

    internal IGraphicsTexture2D Resolve(string source, out bool opaque)
    {
        if (_generation != BAsset.cacheGeneration)
        {
            foreach (var previous in _textures.Values) previous.Texture.Dispose();
            _textures.Clear();
            _generation = BAsset.cacheGeneration;
        }
        opaque = true; // The missing-texture checker is entirely opaque.
        if (string.IsNullOrWhiteSpace(source)) return Missing();
        if (_textures.TryGetValue(source, out var cached))
        { opaque = cached.Opaque; return cached.Texture; }
        if (!_source.TryRead(source, out var bytes)) return Missing();
        IGraphicsTexture2D texture;
        try
        {
            var label = $"BEngine.TiledMap.{_source.GetName(source)}";
            var image = BEngine.Rendering.TextureImageData.Decode(bytes);
            opaque = image.HasOpaqueAlpha();
            texture = BEngine.Rendering.TextureUpload.Create(_device, label, image, TextureFilterMode.Point);
        }
        catch (InvalidDataException) { return Missing(); }
        _textures[source] = new(texture, opaque);
        return texture;
    }

    public void Dispose()
    {
        foreach (var entry in _textures.Values) entry.Texture.Dispose();
        _textures.Clear();
        _missing?.Dispose();
        _missing = null;
    }

    private IGraphicsTexture2D Missing() => _missing ??= _device.CreateTexture2D(
        "BEngine.TiledMap.Missing",
        new GraphicsTextureDescription(2, 2, GraphicsTextureFormat.Rgba8Unorm,
            GraphicsTextureUsage.Sampled, GraphicsTextureFilter.Nearest,
            GraphicsTextureFilter.Nearest, GraphicsTextureAddressMode.ClampToEdge), MissingPixels);

}
