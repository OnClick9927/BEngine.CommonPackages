namespace BEngine.TiledMap;

internal static class TiledMapRuntimeServices
{
    internal static ITilemapTextureSource TextureSource =>
        RuntimeServices.GetService<ITilemapTextureSource>() ??
        FileTilemapTextureSource.Shared;
}
