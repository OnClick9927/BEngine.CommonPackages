using BEngine.Physics2D;

namespace BEngine;

[AddComponentMenu("Rendering/Sprite Shape 2D")]
[DisallowMultipleComponent]
public sealed class SpriteShapeRenderer2D : Renderer2D
{
    private long _collisionSignature = long.MinValue;

    public SpriteShapeAsset? shape { get; set; }
    public bool generateCollider { get; set; } = true;
    public Fix64 colliderEdgeRadius { get; set; } = Fix64.FromRaw(42949672L) /* 0.01 */;
    public Color color { get; set; } = Color.white;

    public SpriteShapeGeometry BuildGeometry() => SpriteShapeGeometryBuilder.Build(
        shape ?? throw new InvalidOperationException("SpriteShapeRenderer2D has no shape asset."));

    public bool RebuildCollider()
    {
        if (!generateCollider || shape is null) return false;
        var geometry = BuildGeometry();
        if (shape.Closed)
        {
            var polygon = gameObject.GetComponent<PolygonCollider2D>() ?? gameObject.AddComponent<PolygonCollider2D>();
            polygon.points = geometry.Outline.ToList();
            polygon.holes = Gather1073(shape.Holes);
            [global::BEngine.SkipLoopGuard]
            global::System.Collections.Generic.List<global::BEngine.Physics2D.ColliderPath2D> Gather1073(global::System.Collections.Generic.IEnumerable<global::BEngine.SpriteShapeHole> items1073)
            {
                ArgumentNullException.ThrowIfNull(items1073, "source");
                global::BEngine.Physics2D.ColliderPath2D Evaluate1073(global::BEngine.SpriteShapeHole hole) => new ColliderPath2D
                {
                    isHole = true,
                    points = hole.Points.ToList()
                };
                global::System.ReadOnlySpan<global::BEngine.SpriteShapeHole> values;
                if (items1073.GetType() == typeof(global::BEngine.SpriteShapeHole[]))
                    values = (global::BEngine.SpriteShapeHole[])items1073;
                else if (items1073.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.SpriteShapeHole>))
                    values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.SpriteShapeHole>)items1073);
                else
                {
                    return global::System.Linq.Enumerable.ToList(global::System.Linq.Enumerable.Select(items1073, Evaluate1073));
                }

                var result = new global::System.Collections.Generic.List<global::BEngine.Physics2D.ColliderPath2D>(values.Length);
                global::System.Runtime.InteropServices.CollectionsMarshal.SetCount(result, values.Length);
                var destination = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(result);
                for (var index = 0; index < values.Length; index++)
                    destination[index] = Evaluate1073(values[index]);
                return result;
            }
            polygon.enabled = true;
            var edge = gameObject.GetComponent<EdgeCollider2D>();
            if (edge is not null) edge.enabled = false;
        }
        else
        {
            var edge = gameObject.GetComponent<EdgeCollider2D>() ?? gameObject.AddComponent<EdgeCollider2D>();
            edge.points = geometry.Outline.ToList();
            edge.edgeRadius = Fix64.Max(Fix64.FromRaw(429496L) /* 0.0001 */, colliderEdgeRadius);
            edge.enabled = true;
            var polygon = gameObject.GetComponent<PolygonCollider2D>();
            if (polygon is not null) polygon.enabled = false;
        }
        _collisionSignature = CollisionSignature(geometry.Outline, shape);
        return true;
    }

    public override void LateUpdate()
    {
        if (!generateCollider || shape is null) return;
        try
        {
            var outline = SpriteShapeGeometryBuilder.Sample(shape);
            var signature = CollisionSignature(outline, shape);
            if (signature != _collisionSignature) RebuildCollider();
        }
        catch (InvalidDataException) { }
    }

    public override void OnValidate()
    {
        colliderEdgeRadius = Fix64.Max(Fix64.FromRaw(429496L) /* 0.0001 */, colliderEdgeRadius);
        if (shape is null) return;
        shape.Validate();
        if (generateCollider) RebuildCollider();
    }

    private static long CollisionSignature(IReadOnlyList<Vector2> points, SpriteShapeAsset asset)
    {
        var hash = new HashCode();
        hash.Add(asset.Closed);
        hash.Add(points.Count);
        foreach (var point in points) { hash.Add(point.x.RawValue); hash.Add(point.y.RawValue); }
        foreach (var hole in asset.Holes)
        {
            hash.Add(hole.Points.Count);
            foreach (var point in hole.Points) { hash.Add(point.x.RawValue); hash.Add(point.y.RawValue); }
        }
        return hash.ToHashCode();
    }
}
