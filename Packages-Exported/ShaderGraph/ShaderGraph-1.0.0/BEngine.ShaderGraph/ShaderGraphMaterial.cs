namespace BEngine.ShaderGraph;

public sealed class ShaderTextureValue
{
    public string name { get; set; } = "Texture";
    public Texture? texture { get; set; }
}

/// <summary>A shared, serializable set of graph properties. Renderer overrides take precedence.</summary>
[CreateAssetMenu(fileName = "New Shader Graph Material", menuName = "Rendering/Shader Graph Material", order = 206)]
[EditorIcon("Icons/Assets/AssetMaterial.png")]
public sealed class ShaderGraphMaterial : ScriptableObject
{
    public ShaderGraphAsset? graph { get; set; }
    public List<ShaderParameterValue> parameters { get; set; } = [];
    public List<ShaderTextureValue> textures { get; set; } = [];

    public void SetParameter(string name, Color value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var item = Matches821();
        [global::BEngine.SkipLoopGuard]
        global::BEngine.ShaderGraph.ShaderParameterValue? Matches821()
        {
            var matchSource821 = parameters;
            ArgumentNullException.ThrowIfNull(matchSource821, "source");
            if (matchSource821.GetType() != typeof(global::System.Collections.Generic.List<global::BEngine.ShaderGraph.ShaderParameterValue>))
            {
                foreach (var item in (global::System.Collections.Generic.IEnumerable<global::BEngine.ShaderGraph.ShaderParameterValue>)matchSource821)
                {
                    if (item.name == name)
                        return item;
                }
        
                return default!;
            }
        
            global::System.ReadOnlySpan<global::BEngine.ShaderGraph.ShaderParameterValue> matchValues821 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(matchSource821);
            foreach (var item in matchValues821)
            {
                if (item.name == name)
                    return item;
            }
        
            return default!;
        }
        if (item is null) parameters.Add(new() { name = name, value = value });
        else item.value = value;
    }

    public void SetTexture(string name, Texture? texture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var item = Matches1135();
        [global::BEngine.SkipLoopGuard]
        global::BEngine.ShaderGraph.ShaderTextureValue? Matches1135()
        {
            var matchSource1135 = textures;
            ArgumentNullException.ThrowIfNull(matchSource1135, "source");
            if (matchSource1135.GetType() != typeof(global::System.Collections.Generic.List<global::BEngine.ShaderGraph.ShaderTextureValue>))
            {
                foreach (var item in (global::System.Collections.Generic.IEnumerable<global::BEngine.ShaderGraph.ShaderTextureValue>)matchSource1135)
                {
                    if (item.name == name)
                        return item;
                }
        
                return default!;
            }
        
            global::System.ReadOnlySpan<global::BEngine.ShaderGraph.ShaderTextureValue> matchValues1135 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(matchSource1135);
            foreach (var item in matchValues1135)
            {
                if (item.name == name)
                    return item;
            }
        
            return default!;
        }
        if (item is null) textures.Add(new() { name = name, texture = texture });
        else item.texture = texture;
    }
}
