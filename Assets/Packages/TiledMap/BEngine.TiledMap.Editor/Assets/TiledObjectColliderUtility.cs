using BEngine.Physics2D;

namespace BEngine.TiledMap.Editor;

/// <summary>Creates independent collider geometry; point markers remain non-physical.</summary>
public static class TiledObjectColliderUtility
{
    public static Collider2D? Create(TiledObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.tileGid != 0 && item.GetComponent<Tilemap>() is { } map && map.GetCell(new(0, 0)) is { } cell &&
            map.TryGetPalette(out var palette) && palette.Find(cell.TileId)?.CollisionPaths is { } paths)
        {
            Collider2D? first = null;
            var closed = paths.Where(path => path.Closed).ToArray();
            if (closed.Length > 0)
            {
                var composite = item.gameObject.AddComponent<CompositeCollider2D>();
                composite.paths = Gather823(closed);
                [global::BEngine.SkipLoopGuard]
                global::System.Collections.Generic.List<global::BEngine.Physics2D.ColliderPath2D> Gather823(global::System.Collections.Generic.IEnumerable<global::BEngine.TiledMap.TileCollisionPath2D> items823)
                {
                    ArgumentNullException.ThrowIfNull(items823, "source");
                    global::BEngine.Physics2D.ColliderPath2D Evaluate823(global::BEngine.TiledMap.TileCollisionPath2D path) => new ColliderPath2D
                    {
                        points = path.TransformPoints(item.size, map.CellToLocal(new(0, 0)), cell.Transform),
                        isHole = path.IsHole
                    };
                    global::System.ReadOnlySpan<global::BEngine.TiledMap.TileCollisionPath2D> values;
                    if (items823.GetType() == typeof(global::BEngine.TiledMap.TileCollisionPath2D[]))
                        values = (global::BEngine.TiledMap.TileCollisionPath2D[])items823;
                    else if (items823.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.TiledMap.TileCollisionPath2D>))
                        values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.TiledMap.TileCollisionPath2D>)items823);
                    else
                    {
                        return global::System.Linq.Enumerable.ToList(global::System.Linq.Enumerable.Select(items823, Evaluate823));
                    }

                    var result = new global::System.Collections.Generic.List<global::BEngine.Physics2D.ColliderPath2D>(values.Length);
                    global::System.Runtime.InteropServices.CollectionsMarshal.SetCount(result, values.Length);
                    var destination = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(result);
                    for (var index = 0; index < values.Length; index++)
                        destination[index] = Evaluate823(values[index]);
                    return result;
                }
                composite.isTrigger = IsTrigger(item); first = composite;
            }
            {
                var __filteredSource1086 = paths;
                ArgumentNullException.ThrowIfNull(__filteredSource1086, "source");
                foreach (var path in __filteredSource1086)
                {
                    if (!(!path.Closed))
                        continue;
                    var edge = item.gameObject.AddComponent<EdgeCollider2D>();
                    edge.points = path.TransformPoints(item.size, map.CellToLocal(new(0, 0)), cell.Transform);
                    edge.isTrigger = IsTrigger(item);
                    first ??= edge;
                }
            }
            return first;
        }
        if (item.shape == TiledObjectShape.Point) return null;
        var outline = item.GetLocalOutline().ToList();
        if (outline.Count < 2 || item.shape is TiledObjectShape.Rectangle or TiledObjectShape.Ellipse && (item.size.x <= 0 || item.size.y <= 0)) return null;
        Collider2D result;
        if (item.shape == TiledObjectShape.Polyline)
        {
            var edge = item.gameObject.AddComponent<EdgeCollider2D>(); edge.points = outline; result = edge;
        }
        else
        {
            var polygon = item.gameObject.AddComponent<PolygonCollider2D>(); polygon.points = outline; result = polygon;
        }
        result.isTrigger = IsTrigger(item);
        return result;
    }
    private static bool IsTrigger(TiledObject item) => string.Equals(item.GetProperty("isTrigger"), "true", StringComparison.OrdinalIgnoreCase);
}
