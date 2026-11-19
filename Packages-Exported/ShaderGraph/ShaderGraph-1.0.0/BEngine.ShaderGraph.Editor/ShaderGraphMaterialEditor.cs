using BEngine.Editor;
using BEngine.Rendering.Rhi;

namespace BEngine.ShaderGraph.Editor;

[CustomEditor(typeof(ShaderGraphMaterial))]
public sealed class ShaderGraphMaterialEditor : BAssetEditor
{
    public override void OnInspectorGUI()
    {
        var material = (ShaderGraphMaterial)target;
        ShaderGraphAsset? graph;
        using (new EditorGUI.DisabledScope(!EditorAssetWritePolicy.CanWrite))
        {
            graph = EditorGUILayout.ObjectField("Graph", material.graph, false);
            if (!ReferenceEquals(graph, material.graph)) Change("Change material graph", () => material.graph = graph);
        }
        if (graph is null) return;
        if (GUILayout.Button("Open Shader Graph")) ShaderGraphWindow.Open(graph);
        if (GUILayout.Button("GPU Preview")) ShaderGraphPreviewWindow.Open(graph).SetMaterial(material);
        CompiledShaderGraph compiled;
        try { compiled = ShaderGraphCompiler.Compile(graph, GraphicsBackend.Vulkan); }
        catch (Exception exception) { EditorGUILayout.HelpBox(exception.Message, MessageType.Error); return; }
        using (new EditorGUI.DisabledScope(!EditorAssetWritePolicy.CanWrite))
        {
            foreach (var parameter in compiled.Parameters)
            {
                [global::BEngine.SkipLoopGuard]
                global::BEngine.ShaderGraph.ShaderParameterValue? Query1277(global::System.Collections.Generic.IEnumerable<global::BEngine.ShaderGraph.ShaderParameterValue> querySource1277)
                {
                    ArgumentNullException.ThrowIfNull(querySource1277, "source");
                    bool Matches1277(global::BEngine.ShaderGraph.ShaderParameterValue item) => item.name == parameter.Name;
                    global::System.ReadOnlySpan<global::BEngine.ShaderGraph.ShaderParameterValue> queryValues1277;
                    if (querySource1277.GetType() == typeof(global::BEngine.ShaderGraph.ShaderParameterValue[]))
                        queryValues1277 = (global::BEngine.ShaderGraph.ShaderParameterValue[])querySource1277;
                    else if (querySource1277.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.ShaderGraph.ShaderParameterValue>))
                        queryValues1277 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.ShaderGraph.ShaderParameterValue>)querySource1277);
                    else
                    {
                        foreach (var item in querySource1277)
                            if (Matches1277(item))
                                return item;
                        return default!;
                    }
                
                    foreach (var item in queryValues1277)
                        if (Matches1277(item))
                            return item;
                    return default!;
                }
                
                var value = Query1277(material.parameters)?.value ?? parameter.DefaultValue;
                var next = EditorGUILayout.ColorField(parameter.Name, value);
                if (!value.Equals(next)) Change("Change material parameter", () => material.SetParameter(parameter.Name, next));
            }
            foreach (var parameter in compiled.Textures)
            {
                [global::BEngine.SkipLoopGuard]
                global::BEngine.ShaderGraph.ShaderTextureValue? Query1703(global::System.Collections.Generic.IEnumerable<global::BEngine.ShaderGraph.ShaderTextureValue> querySource1703)
                {
                    ArgumentNullException.ThrowIfNull(querySource1703, "source");
                    bool Matches1703(global::BEngine.ShaderGraph.ShaderTextureValue item) => item.name == parameter.Name;
                    global::System.ReadOnlySpan<global::BEngine.ShaderGraph.ShaderTextureValue> queryValues1703;
                    if (querySource1703.GetType() == typeof(global::BEngine.ShaderGraph.ShaderTextureValue[]))
                        queryValues1703 = (global::BEngine.ShaderGraph.ShaderTextureValue[])querySource1703;
                    else if (querySource1703.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.ShaderGraph.ShaderTextureValue>))
                        queryValues1703 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.ShaderGraph.ShaderTextureValue>)querySource1703);
                    else
                    {
                        foreach (var item in querySource1703)
                            if (Matches1703(item))
                                return item;
                        return default!;
                    }
                
                    foreach (var item in queryValues1703)
                        if (Matches1703(item))
                            return item;
                    return default!;
                }
                
                var entry = Query1703(material.textures);
                var value = entry is null ? parameter.DefaultTexture : entry.texture;
                var next = EditorGUILayout.ObjectField(parameter.Name, value, false);
                if (!ReferenceEquals(value, next)) Change("Change material texture", () => material.SetTexture(parameter.Name, next));
            }
            if (GUILayout.Button("Reset overrides")) Change("Reset material overrides", () => { material.parameters.Clear(); material.textures.Clear(); });
        }
    }

    private void Change(string label, Action action)
    {
        EditorAssetWritePolicy.EnsureCanWrite(label);
        Undo.IncrementCurrentGroup(); Undo.RecordObject(target, label); action(); EditorUtility.SetDirty(target);
    }
}
