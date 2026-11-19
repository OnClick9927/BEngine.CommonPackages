using BEngine.ProjectSystem.Editor;
using BEngine.Rendering.Rhi;
namespace BEngine.Editor;
public sealed class SpriteSkinShaderBuildSources : IShaderSourceProvider
{
    public IEnumerable<ShaderBuildSource> CollectSources(IReadOnlyList<AssetRecord> assets,
        IReadOnlyList<ShaderBuildResourceInput> resources, GraphicsBackend backend)
    {
        bool __queryResult369;
        {
            var __querySource369 = resources;
            ArgumentNullException.ThrowIfNull(__querySource369, "source");
            __queryResult369 = false;
            var __querySource369Type = __querySource369.GetType();
            if (__querySource369Type == typeof(BEngine.Editor.ShaderBuildResourceInput[]) || __querySource369Type == typeof(System.Collections.Generic.List<BEngine.Editor.ShaderBuildResourceInput>))
            {
                System.ReadOnlySpan<BEngine.Editor.ShaderBuildResourceInput> __querySource369Span = __querySource369Type == typeof(BEngine.Editor.ShaderBuildResourceInput[]) ? (BEngine.Editor.ShaderBuildResourceInput[])(object)__querySource369 : System.Runtime.InteropServices.CollectionsMarshal.AsSpan((System.Collections.Generic.List<BEngine.Editor.ShaderBuildResourceInput>)(object)__querySource369);
                foreach (var __queryItem369 in __querySource369Span)
                {
                    if ((__queryItem369.Address.EndsWith("/Shaders/SpriteSkinning2D/Textured.vulkan.frag.glsl", StringComparison.OrdinalIgnoreCase)))
                    {
                        __queryResult369 = true;
                        break;
                    }
                }
            }
            else
            {
                foreach (var __queryItem369 in __querySource369)
                {
                    if ((__queryItem369.Address.EndsWith("/Shaders/SpriteSkinning2D/Textured.vulkan.frag.glsl", StringComparison.OrdinalIgnoreCase)))
                    {
                        __queryResult369 = true;
                        break;
                    }
                }
            }
        }
        
        if (!__queryResult369)
            yield break;
        string Load(string path)
        {
            return File.ReadAllText(Gather2182(resources).FilePath);
            [global::BEngine.SkipLoopGuard]
            global::BEngine.Editor.ShaderBuildResourceInput Gather2182(global::System.Collections.Generic.IEnumerable<global::BEngine.Editor.ShaderBuildResourceInput> items2182)
            {
                ArgumentNullException.ThrowIfNull(items2182, "source");
                bool Evaluate2182(global::BEngine.Editor.ShaderBuildResourceInput r) => r.Address.EndsWith("/" + path, StringComparison.OrdinalIgnoreCase);
                global::System.ReadOnlySpan<global::BEngine.Editor.ShaderBuildResourceInput> values;
                if (items2182.GetType() == typeof(global::BEngine.Editor.ShaderBuildResourceInput[]))
                    values = (global::BEngine.Editor.ShaderBuildResourceInput[])items2182;
                else if (items2182.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.Editor.ShaderBuildResourceInput>))
                    values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.Editor.ShaderBuildResourceInput>)items2182);
                else
                    return global::System.Linq.Enumerable.Single(items2182, Evaluate2182);
                var found = false;
                global::BEngine.Editor.ShaderBuildResourceInput result = default!;
                foreach (var r in values)
                {
                    if (!Evaluate2182(r))
                        continue;
                    if (found)
                        return global::System.Linq.Enumerable.Single(new global::BEngine.Editor.ShaderBuildResourceInput[2], static _ => true);
                    result = r;
                    found = true;
                }

                if (found)
                    return result;
                return global::System.Linq.Enumerable.Single(global::System.Array.Empty<global::BEngine.Editor.ShaderBuildResourceInput>(), static _ => true);
            }
        }
        foreach (var capacity in new[] { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024 })
        {
            var pair = BEngine.SpriteSkinShaderSources.Create(backend, capacity, Load);
            yield return ShaderBuildSourceAdapters.FromGraphics(pair.Vertex); yield return ShaderBuildSourceAdapters.FromGraphics(pair.Fragment);
        }
    }
}
