using BEngine.Editor;

namespace BEngine.ShaderGraph.Editor;

internal static class ShaderGraphEditorRegistration
{
    [InitializeOnLoadMethod]
    private static void RegisterIcons() =>
        AssetTypeRegistry.Register(".shadergraph", "Shader Graph", EditorBuiltinIcons.Assets.Shader);
}
