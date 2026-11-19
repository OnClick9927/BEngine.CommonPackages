using BEngine.Rendering;

namespace BEngine.ShaderGraph;

public sealed class ShaderParameterValue
{
    public string name { get; set; } = "Tint";
    public Color value { get; set; } = Color.white;
}

[AddComponentMenu("Rendering/Shader Graph Renderer 2D")]
public sealed class ShaderGraphRenderer2D : Renderer2D, ISpriteReferenceOwner
{
    public ShaderGraphRenderer2D() { receiveLighting = true; }
    public ShaderGraphAsset? graph { get; set; }
    public ShaderGraphMaterial? shaderMaterial { get; set; }
    public Sprite? sprite { get; set; }
    public Vector2 size { get; set; } = Vector2.one;
    public Color color { get; set; } = Color.white;
    public bool flipX { get; set; }
    public bool flipY { get; set; }
    public Fix64 timeScale { get; set; } = 1;
    public Fix64 timeOffset { get; set; }
    public List<ShaderParameterValue> parameters { get; set; } = [];
    public List<ShaderTextureValue> textures { get; set; } = [];

    public ShaderGraphAsset? ResolveGraph() => graph is { isDestroyed: false } ? graph :
        shaderMaterial is { isDestroyed: false } ? shaderMaterial.graph : null;

    public void SetTexture(string name, Texture? texture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var item = Matches1263();
        [global::BEngine.SkipLoopGuard]
        global::BEngine.ShaderGraph.ShaderTextureValue? Matches1263()
        {
            var matchSource1263 = textures;
            ArgumentNullException.ThrowIfNull(matchSource1263, "source");
            if (matchSource1263.GetType() != typeof(global::System.Collections.Generic.List<global::BEngine.ShaderGraph.ShaderTextureValue>))
            {
                foreach (var item in (global::System.Collections.Generic.IEnumerable<global::BEngine.ShaderGraph.ShaderTextureValue>)matchSource1263)
                {
                    if (item.name == name)
                        return item;
                }
        
                return default!;
            }
        
            global::System.ReadOnlySpan<global::BEngine.ShaderGraph.ShaderTextureValue> matchValues1263 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(matchSource1263);
            foreach (var item in matchValues1263)
            {
                if (item.name == name)
                    return item;
            }
        
            return default!;
        }
        if (item is null) textures.Add(new() { name = name, texture = texture });
        else item.texture = texture;
    }

    public void SetParameter(string name, Color value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var parameter = Matches1583();
        [global::BEngine.SkipLoopGuard]
        global::BEngine.ShaderGraph.ShaderParameterValue? Matches1583()
        {
            var matchSource1583 = parameters;
            ArgumentNullException.ThrowIfNull(matchSource1583, "source");
            if (matchSource1583.GetType() != typeof(global::System.Collections.Generic.List<global::BEngine.ShaderGraph.ShaderParameterValue>))
            {
                foreach (var item in (global::System.Collections.Generic.IEnumerable<global::BEngine.ShaderGraph.ShaderParameterValue>)matchSource1583)
                {
                    if (item.name == name)
                        return item;
                }
        
                return default!;
            }
        
            global::System.ReadOnlySpan<global::BEngine.ShaderGraph.ShaderParameterValue> matchValues1583 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(matchSource1583);
            foreach (var item in matchValues1583)
            {
                if (item.name == name)
                    return item;
            }
        
            return default!;
        }
        if (parameter is null) parameters.Add(new() { name = name, value = value });
        else parameter.value = value;
    }

    public override void CollectGeometry(ISceneGeometrySink2D sink)
    {
        if (ResolveGraph() is not { isDestroyed: false } || !RuntimePackageState.IsEnabled("com.bengine.shader-graph")) return;
        var visual = SpriteSourceRegistry.Resolve(sprite);
        var scale = transform.lossyScale;
        var dimensions = Vector2.Scale(size, scale);
        var offset = Vector2.Scale(new Vector2(Fix64.Half - visual.Pivot.x, Fix64.Half - visual.Pivot.y), size);
        var quad = SceneQuad2D.Create(sink.Camera.SnapWorldPoint(transform.TransformPoint(offset)),
            transform.rotation, new(Fix64.Abs(dimensions.x), Fix64.Abs(dimensions.y)),
            new(color.r, color.g, color.b, color.a * opacity), visual.Texture, visual.Uv,
            flipX ^ (dimensions.x < 0), flipY ^ (dimensions.y < 0), visual.IsTextured) with { EffectData = this, ApplySurfaceEffects = false };
        var matrix = transform.localToWorldMatrix;
        var axisX = matrix.TransformVector(new Vector2(Fix64.Abs(size.x) * Fix64.Half, 0));
        var axisY = matrix.TransformVector(new Vector2(0, Fix64.Abs(size.y) * Fix64.Half));
        if (scale.x < 0) axisX = -axisX;
        if (scale.y < 0) axisY = -axisY;
        quad = quad with { WorldAxisX = axisX, WorldAxisY = axisY };
        // Only the shared default unlit state can merge. Independent overrides retain their identity.
        var shared = ResolveGraph() is { surface: ShaderGraphSurface.Unlit } current &&
            shaderMaterial is null && parameters.Count == 0 && textures.Count == 0 && timeScale == 1 && timeOffset == 0
            ? current.Id : Id;
        sink.Submit(this, quad, new RenderBatchKey(shared, "BEngine/ShaderGraph", visual.BatchIdentity));
    }
}
