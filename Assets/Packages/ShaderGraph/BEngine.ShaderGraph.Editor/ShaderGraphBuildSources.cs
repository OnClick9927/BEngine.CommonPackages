using BEngine.Editor;
using BEngine.ProjectSystem.Editor;
using BEngine.Rendering.Rhi;
using BEngine.Serialization;

namespace BEngine.ShaderGraph.Editor;

public sealed class ShaderGraphBuildSources : IShaderSourceProvider
{
    public IEnumerable<ShaderBuildSource> CollectSources(IReadOnlyList<AssetRecord> assets, IReadOnlyList<ShaderBuildResourceInput> resources, GraphicsBackend backend)
    {
        {
            var __filteredSource419 = assets;
            ArgumentNullException.ThrowIfNull(__filteredSource419, "source");
            foreach (var asset in __filteredSource419)
            {
                if (!(asset.SourcePath.EndsWith(".asset.yaml", StringComparison.OrdinalIgnoreCase)))
                    continue;
                var yaml = File.ReadAllText(asset.ArtifactPath);
                if (ManagedAssetSerialization.GetAssetType(yaml) != typeof(ShaderGraphAsset))
                    continue;
                var graph = ManagedAssetSerialization.Deserialize<ShaderGraphAsset>(yaml);
                try
                {
                    var evaluation = Gather1100(resources);
                    [global::BEngine.SkipLoopGuard]
                    global::BEngine.Editor.ShaderBuildResourceInput? Gather1100(global::System.Collections.Generic.IEnumerable<global::BEngine.Editor.ShaderBuildResourceInput> items1100)
                    {
                        ArgumentNullException.ThrowIfNull(items1100, "source");
                        bool Evaluate1100(global::BEngine.Editor.ShaderBuildResourceInput r) => r.Address.EndsWith("/Shaders/PortableScene/Lighting.evaluation.glslinc", StringComparison.OrdinalIgnoreCase);
                        global::System.ReadOnlySpan<global::BEngine.Editor.ShaderBuildResourceInput> values;
                        if (items1100.GetType() == typeof(global::BEngine.Editor.ShaderBuildResourceInput[]))
                            values = (global::BEngine.Editor.ShaderBuildResourceInput[])items1100;
                        else if (items1100.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.Editor.ShaderBuildResourceInput>))
                            values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.Editor.ShaderBuildResourceInput>)items1100);
                        else
                            return global::System.Linq.Enumerable.FirstOrDefault(items1100, Evaluate1100);
                        foreach (var r in values)
                            if (Evaluate1100(r))
                                return r;
                        return default!;
                    }
                    var functions = graph.surface == ShaderGraphSurface.Lit2D ? File.ReadAllText(evaluation?.FilePath ?? throw new InvalidDataException("Lit graph requires the Lighting2D evaluation resource.")) : null;
                    var unlit = ShaderGraphCompiler.CompileSurface(graph, backend, null, 0, 0);
                    yield return ShaderBuildSourceAdapters.FromGraphics(unlit.Program.VertexShader);
                    yield return ShaderBuildSourceAdapters.FromGraphics(unlit.Program.FragmentShader);
                    if (graph.surface != ShaderGraphSurface.Lit2D)
                        continue;
                    var parameterCapacity = Math.Max(1, unlit.Parameters.Count);
                    foreach (var capacity in new[]
                    {
                        32,
                        64,
                        128,
                        256,
                        512,
                        1024,
                        2048,
                        4079
                    }
        
                    )
                    {
                        if (16 + (parameterCapacity + capacity) * 16 > 65536)
                            continue;
                        for (var cookies = 0; cookies <= Math.Min(6, 7 - unlit.Textures.Count); cookies++)
                        {
                            var program = ShaderGraphCompiler.CompileSurface(graph, backend, null, capacity, cookies, functions).Program;
                            yield return ShaderBuildSourceAdapters.FromGraphics(program.VertexShader);
                            yield return ShaderBuildSourceAdapters.FromGraphics(program.FragmentShader);
                        }
                    }
                }
                finally
                {
                    BObject.DestroyImmediate(graph);
                }
            }
        }
    }
}
