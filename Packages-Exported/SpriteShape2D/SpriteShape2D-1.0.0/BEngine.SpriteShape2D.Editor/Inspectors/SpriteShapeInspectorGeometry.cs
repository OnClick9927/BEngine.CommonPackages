namespace BEngine.Editor;

// Owned by one Inspector. Keep only the displayed counts and exact input values,
// rather than retaining the generated mesh or relying on mutable-list identities.
internal sealed class SpriteShapeInspectorGeometry
{
    private SpriteShapeAsset? _asset;
    private Settings _settings;
    private Point[] _points = [];
    private Vector2[][] _holes = [];
    private Corner[] _corners = [];
    private string _summary = string.Empty;

    internal string GetSummary(SpriteShapeAsset asset)
    {
        if (Matches(asset)) return _summary;
        Clear();
        var geometry = SpriteShapeGeometryBuilder.Build(asset);
        var summary = $"{geometry.Outline.Length} outline points, {geometry.EdgeTriangles.Length / 3} edge triangles, {geometry.FillTriangles.Length / 3} fill triangles";

        // Build validates and normalizes the asset. Capture those final values so
        // later direct edits (including Undo restoration) always trigger a build.
        _settings = new Settings(asset);
        _points = new Point[asset.Points.Count];
        for (var index = 0; index < _points.Length; index++) _points[index] = new Point(asset.Points[index]);
        _holes = new Vector2[asset.Holes.Count][];
        for (var index = 0; index < _holes.Length; index++) _holes[index] = asset.Holes[index].Points.ToArray();
        _corners = new Corner[asset.CornerRules.Count];
        for (var index = 0; index < _corners.Length; index++) _corners[index] = new Corner(asset.CornerRules[index]);
        _summary = summary;
        _asset = asset;
        return summary;
    }

    internal void Clear()
    {
        _asset = null;
        _settings = default;
        _points = [];
        _holes = [];
        _corners = [];
        _summary = string.Empty;
    }

    private bool Matches(SpriteShapeAsset asset)
    {
        if (!ReferenceEquals(_asset, asset) || !_settings.Equals(new Settings(asset)) ||
            asset.Points is null || asset.Points.Count != _points.Length ||
            asset.Holes is null || asset.Holes.Count != _holes.Length ||
            asset.CornerRules is null || asset.CornerRules.Count != _corners.Length) return false;
        for (var index = 0; index < _points.Length; index++)
            if (asset.Points[index] is not { } point || !_points[index].Equals(new Point(point))) return false;
        for (var index = 0; index < _holes.Length; index++)
        {
            if (asset.Holes[index]?.Points is not { } points || points.Count != _holes[index].Length) return false;
            for (var point = 0; point < points.Count; point++)
                if (points[point] != _holes[index][point]) return false;
        }
        for (var index = 0; index < _corners.Length; index++)
            if (asset.CornerRules[index] is not { } rule || !_corners[index].Equals(new Corner(rule))) return false;
        return true;
    }

    private readonly record struct Point(Vector2 Position, Vector2 Left, Vector2 Right, SpriteShapeTangentMode Mode)
    {
        internal Point(SpriteShapePoint point) : this(point.Position, point.LeftTangent, point.RightTangent, point.Mode) { }
    }

    private readonly record struct Corner(Fix64 Minimum, Fix64 Maximum, bool Inner, string Texture, Rect Uv, Vector2 Size, Color Color)
    {
        internal Corner(SpriteShapeCornerRule rule) : this(rule.MinimumAngle, rule.MaximumAngle, rule.InnerCorner,
            rule.Texture, rule.Uv, rule.Size, rule.Color) { }
    }

    private readonly record struct Settings(string Format, int Version, bool Closed, int Samples, bool Adaptive,
        Fix64 Tolerance, int Depth, Fix64 Width, Fix64 Repeat, string EdgeTexture, Rect EdgeUv, Color EdgeColor,
        string FillTexture, Rect FillUv, Color FillColor)
    {
        internal Settings(SpriteShapeAsset asset) : this(asset.Format, asset.Version, asset.Closed,
            asset.SamplesPerSegment, asset.AdaptiveTessellation, asset.FlatnessTolerance, asset.MaxSubdivisionDepth,
            asset.EdgeWidth, asset.EdgeRepeatLength, asset.EdgeTexture, asset.EdgeUv, asset.EdgeColor,
            asset.FillTexture, asset.FillUv, asset.FillColor) { }
    }
}
