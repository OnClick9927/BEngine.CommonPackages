using BEngine.Serialization;

namespace BEngine.TiledMap;

[EditorIcon("Icons/Assets/AssetImage.png")]
public sealed class TilePalette : ScriptableObject
{
    public string Format { get; set; } = "BEngine.TilePalette";
    public int Version { get; set; } = 1;
    [YamlDotNet.Serialization.YamlIgnore, HideInInspector]
    public string Name { get => name; set => name = value; }
    public string Atlas { get; set; } = string.Empty;
    public int Columns { get; set; } = 1;
    public int Rows { get; set; } = 1;
    public List<TileDefinition> Tiles { get; set; } = [];

    // Null preserves the automatic atlas layout of older palettes; an empty list is an intentionally empty palette.
    public List<TilePaletteCell>? Layout { get; set; }

    public TileDefinition? Find(int id) => Tiles.FirstOrDefault(tile => tile.Id == id);

    public void Validate()
    {
        if (!string.Equals(Format, "BEngine.TilePalette", StringComparison.Ordinal) || Version != 1)
            throw new InvalidDataException($"Unsupported tile palette format/version '{Format}' v{Version}.");
        if (string.IsNullOrWhiteSpace(Name)) Name = "Tile Palette";
        Columns = Math.Max(1, Columns);
        Rows = Math.Max(1, Rows);
        Tiles ??= [];
        if (Tiles.Count > 65536 || Matches1282())
            throw new InvalidDataException("Invalid tile palette entries.");
        [global::BEngine.SkipLoopGuard]
        bool Matches1282()
        {
            var matchSource1282 = Tiles;
            ArgumentNullException.ThrowIfNull(matchSource1282, "source");
            if (matchSource1282.GetType() != typeof(global::System.Collections.Generic.List<global::BEngine.TiledMap.TileDefinition>))
            {
                foreach (var tile in (global::System.Collections.Generic.IEnumerable<global::BEngine.TiledMap.TileDefinition>)matchSource1282)
                {
                    if (tile is null)
                        return true;
                }
        
                return false;
            }
        
            global::System.ReadOnlySpan<global::BEngine.TiledMap.TileDefinition> matchValues1282 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(matchSource1282);
            foreach (var tile in matchValues1282)
            {
                if (tile is null)
                    return true;
            }
        
            return false;
        }
        using var budget = LoopExecutionGuard.AllowIterations(Math.Max(Tiles.Count, Layout?.Count ?? 0) + 1);
        foreach (var tile in Tiles) tile.Validate();
        if (Layout is { } layout && (layout.Count > 65536 || Query2627(layout) || (((global::System.Collections.Generic.IEnumerable<(int X, int Y)>)(layout.Select(cell => (cell.X, cell.Y)))) switch
        {
            (int X, int Y)[] { Length: 0 } => 0,
            var distinctItems2694 => distinctItems2694.ToHashSet<(int X, int Y)>().Count
        }) != layout.Count))
            throw new InvalidDataException("Invalid tile palette layout.");
        [global::BEngine.SkipLoopGuard]
        bool Query2627(global::System.Collections.Generic.IEnumerable<global::BEngine.TiledMap.TilePaletteCell> querySource2627)
        {
            ArgumentNullException.ThrowIfNull(querySource2627, "source");
            bool Matches2627(global::BEngine.TiledMap.TilePaletteCell cell) => cell is null || cell.TileId < 0;
            global::System.ReadOnlySpan<global::BEngine.TiledMap.TilePaletteCell> queryValues2627;
            if (querySource2627.GetType() == typeof(global::BEngine.TiledMap.TilePaletteCell[]))
                queryValues2627 = (global::BEngine.TiledMap.TilePaletteCell[])querySource2627;
            else if (querySource2627.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.TiledMap.TilePaletteCell>))
                queryValues2627 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.TiledMap.TilePaletteCell>)querySource2627);
            else
            {
                foreach (var cell in querySource2627)
                    if (Matches2627(cell))
                        return true;
                return false;
            }
        
            foreach (var cell in queryValues2627)
                if (Matches2627(cell))
                    return true;
            return false;
        }
        var duplicate = Query3103(Tiles.GroupBy(tile => tile.Id));
        [global::BEngine.SkipLoopGuard]
        global::System.Linq.IGrouping<int, global::BEngine.TiledMap.TileDefinition>? Query3103(global::System.Collections.Generic.IEnumerable<global::System.Linq.IGrouping<int, global::BEngine.TiledMap.TileDefinition>> querySource3103)
        {
            ArgumentNullException.ThrowIfNull(querySource3103, "source");
            bool Matches3103(global::System.Linq.IGrouping<int, global::BEngine.TiledMap.TileDefinition> group) => group.Count() > 1;
            global::System.ReadOnlySpan<global::System.Linq.IGrouping<int, global::BEngine.TiledMap.TileDefinition>> queryValues3103;
            if (querySource3103.GetType() == typeof(global::System.Linq.IGrouping<int, global::BEngine.TiledMap.TileDefinition>[]))
                queryValues3103 = (global::System.Linq.IGrouping<int, global::BEngine.TiledMap.TileDefinition>[])querySource3103;
            else if (querySource3103.GetType() == typeof(global::System.Collections.Generic.List<global::System.Linq.IGrouping<int, global::BEngine.TiledMap.TileDefinition>>))
                queryValues3103 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::System.Linq.IGrouping<int, global::BEngine.TiledMap.TileDefinition>>)querySource3103);
            else
            {
                foreach (var group in querySource3103)
                    if (Matches3103(group))
                        return group;
                return default!;
            }
        
            foreach (var group in queryValues3103)
                if (Matches3103(group))
                    return group;
            return default!;
        }
        if (duplicate is not null) throw new InvalidDataException($"Tile ID {duplicate.Key} is duplicated.");
        var ids = BuildIndex3034(Tiles);
        [global::BEngine.SkipLoopGuard]
        global::System.Collections.Generic.HashSet<int> BuildIndex3034(global::System.Collections.Generic.List<global::BEngine.TiledMap.TileDefinition> indexSource3034)
        {
            ArgumentNullException.ThrowIfNull(indexSource3034, "source");
            var indexComparer3034 = (global::System.Collections.Generic.IEqualityComparer<int>? )null;
            var result = new global::System.Collections.Generic.HashSet<int>(indexComparer3034);
            foreach (var tile in indexSource3034)
                result.Add(tile.Id);
            return result;
        }
        foreach (var tile in Tiles)
            if (tile.Rules.Any(rule => !ids.Contains(rule.OutputTileId)) || tile.Animation.Any(frame => !ids.Contains(frame.TileId)))
                throw new InvalidDataException($"Tile {tile.Id} references a missing rule output or animation frame.");
        Tiles = Tiles.OrderBy(tile => tile.Id).ToList();
    }

    public void SliceAtlas(int columns, int rows, int tileCount = -1)
    {
        Layout = null;
        Columns = Math.Max(1, columns);
        Rows = Math.Max(1, rows);
        var count = tileCount < 0 ? checked(Columns * Rows) : Math.Clamp(tileCount, 0, checked(Columns * Rows));
        if (count > 65536) throw new ArgumentOutOfRangeException(nameof(tileCount), "A palette is limited to 65536 tiles.");
        var previous = BuildIndex3865(Tiles, null);
        [global::BEngine.SkipLoopGuard]
        global::System.Collections.Generic.Dictionary<int, global::BEngine.TiledMap.TileDefinition> BuildIndex3865(global::System.Collections.Generic.IEnumerable<global::BEngine.TiledMap.TileDefinition> indexSource3865, global::System.Collections.Generic.IEqualityComparer<int>? indexComparer3865)
        {
            ArgumentNullException.ThrowIfNull(indexSource3865, "source");
            var knownCount = indexSource3865.TryGetNonEnumeratedCount(out var capacity);
            if (knownCount && capacity == 0)
                return new global::System.Collections.Generic.Dictionary<int, global::BEngine.TiledMap.TileDefinition>(indexComparer3865);
            global::System.ReadOnlySpan<global::BEngine.TiledMap.TileDefinition> values;
            if (knownCount && indexSource3865 is global::BEngine.TiledMap.TileDefinition[] array)
                values = array;
            else if (knownCount && indexSource3865 is global::System.Collections.Generic.List<global::BEngine.TiledMap.TileDefinition> list)
                values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list);
            else
            {
                var result = new global::System.Collections.Generic.Dictionary<int, global::BEngine.TiledMap.TileDefinition>(capacity, indexComparer3865);
                foreach (var item in indexSource3865)
                    result.Add(IndexKey3865(item), item);
                return result;
            }
        
            {
                var result = new global::System.Collections.Generic.Dictionary<int, global::BEngine.TiledMap.TileDefinition>(values.Length, indexComparer3865);
                foreach (var item in values)
                    result.Add(IndexKey3865(item), item);
                return result;
            }
        
            int IndexKey3865(global::BEngine.TiledMap.TileDefinition tile) => tile.Id;
        }
        var width = Fix64.One / Columns;
        var height = Fix64.One / Rows;
        Tiles = Enumerable.Range(0, count).Select(index =>
        {
            var id = index + 1;
            var tile = previous.GetValueOrDefault(id) ?? new TileDefinition { Id = id, Name = $"Tile {id}" };
            tile.UvX = index % Columns * width;
            tile.UvY = index / Columns * height;
            tile.UvWidth = width;
            tile.UvHeight = height;
            return tile;
        }).ToList();
    }

    public void ImportAtlas(TextureAtlas atlas)
    {
        ArgumentNullException.ThrowIfNull(atlas);
        atlas.Validate();
        if (atlas.Sprites.Count == 0 || string.IsNullOrWhiteSpace(atlas.Texture))
            throw new InvalidDataException("Build the texture atlas before importing it into a tile palette.");
        var previousByName = BuildIndex4766(Tiles.GroupBy(tile => tile.Name, StringComparer.Ordinal), StringComparer.Ordinal);
        [global::BEngine.SkipLoopGuard]
        global::System.Collections.Generic.Dictionary<string, global::BEngine.TiledMap.TileDefinition> BuildIndex4766(global::System.Collections.Generic.IEnumerable<global::System.Linq.IGrouping<string, global::BEngine.TiledMap.TileDefinition>> indexSource4766, global::System.Collections.Generic.IEqualityComparer<string>? indexComparer4766)
        {
            ArgumentNullException.ThrowIfNull(indexSource4766, "source");
            var knownCount = indexSource4766.TryGetNonEnumeratedCount(out var capacity);
            if (knownCount && capacity == 0)
                return new global::System.Collections.Generic.Dictionary<string, global::BEngine.TiledMap.TileDefinition>(indexComparer4766);
            global::System.ReadOnlySpan<global::System.Linq.IGrouping<string, global::BEngine.TiledMap.TileDefinition>> values;
            if (knownCount && indexSource4766 is global::System.Linq.IGrouping<string, global::BEngine.TiledMap.TileDefinition>[] array)
                values = array;
            else if (knownCount && indexSource4766 is global::System.Collections.Generic.List<global::System.Linq.IGrouping<string, global::BEngine.TiledMap.TileDefinition>> list)
                values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list);
            else
            {
                var result = new global::System.Collections.Generic.Dictionary<string, global::BEngine.TiledMap.TileDefinition>(capacity, indexComparer4766);
                foreach (var item in indexSource4766)
                    result.Add(IndexKey4766(item), IndexValue4766(item));
                return result;
            }
        
            {
                var result = new global::System.Collections.Generic.Dictionary<string, global::BEngine.TiledMap.TileDefinition>(values.Length, indexComparer4766);
                foreach (var item in values)
                    result.Add(IndexKey4766(item), IndexValue4766(item));
                return result;
            }
        
            string IndexKey4766(global::System.Linq.IGrouping<string, global::BEngine.TiledMap.TileDefinition> group) => group.Key;
            global::BEngine.TiledMap.TileDefinition IndexValue4766(global::System.Linq.IGrouping<string, global::BEngine.TiledMap.TileDefinition> group) => group.First();
        }
        Layout = null;
        Atlas = atlas.Texture;
        Columns = 1;
        Rows = 1;
        Tiles = atlas.Sprites.OrderBy(sprite => sprite.Name, StringComparer.Ordinal)
            .Select((sprite, index) =>
            {
                var previous = previousByName.GetValueOrDefault(sprite.Name);
                var uv = sprite.NormalizedUv(atlas.Width, atlas.Height);
                return new TileDefinition
                {
                    Id = index + 1,
                    Name = sprite.Name,
                    UvX = uv.x,
                    UvY = uv.y,
                    UvWidth = uv.width,
                    UvHeight = uv.height,
                    Red = previous?.Red ?? byte.MaxValue,
                    Green = previous?.Green ?? byte.MaxValue,
                    Blue = previous?.Blue ?? byte.MaxValue,
                    Alpha = previous?.Alpha ?? byte.MaxValue,
                    HasCollider = previous?.HasCollider ?? false,
                    CollisionPaths = previous?.CollisionPaths?.Select(path => new TileCollisionPath2D
                    { Closed = path.Closed, IsHole = path.IsHole, Points = path.Points.ToList() }).ToList()
                };
            }).ToList();
    }

    public static TilePalette Load(string path)
    {
        var resolved = TilePalettePath.Resolve(path);
        var palette = RuntimeServices.GetRequiredService<global::BEngine.Platform.FileUtility>().FileExists(resolved)
            ? YamlUtility.Load<TilePalette>(resolved)
            : YamlUtility.Deserialize<TilePalette>((Resources.Load<TextAsset>(path) ??
                throw new FileNotFoundException("Tile palette is missing from active resources.", path)).text);
        palette.Validate();
        palette.BindAssetReference(path);
        return palette;
    }

    public void Save(string path)
    {
        EnsureAssetCanBeSaved();
        Validate();
        YamlUtility.Save(this, TilePalettePath.Resolve(path));
        BindAssetReference(path);
        BAsset.Invalidate(path);
    }
}

public sealed class TilePaletteCell
{
    public int X { get; set; }
    public int Y { get; set; }
    public int TileId { get; set; }
    public TileTransformFlags Transform { get; set; }
}
