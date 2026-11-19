using System.Runtime.CompilerServices;
namespace BEngine;

internal static class TimelinePackageRegistration
{
    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255", Justification = "Register package codecs when its assembly is loaded.")]
    internal static void RegisterCodec()
    {
        RuntimeAssetCodecRegistry.Register<TimelineAsset>((bytes, _) =>
            TimelineAssetSerialization.Deserialize(RuntimeAssetDecoding.DecodeText(bytes.Span)));
        PlayableDirector.RuntimeMethodPreparation.Prepare();
    }
}
