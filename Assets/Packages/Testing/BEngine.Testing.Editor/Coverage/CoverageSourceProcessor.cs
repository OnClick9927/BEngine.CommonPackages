using System.Runtime.CompilerServices;
using BEngine.ProjectSystem;

namespace BEngine.Testing.Editor;

internal sealed class CoverageSourceProcessor : IScriptSourceProcessor
{
    public string Revision => TestCoverageInstrumenter.Revision;
    public string Transform(string source, string displayPath, IReadOnlyList<string> preprocessorSymbols) =>
        TestCoverageInstrumenter.Instrument(source, displayPath, preprocessorSymbols);

    [ModuleInitializer]
    internal static void Initialize() => ScriptSourceProcessors.Register(new CoverageSourceProcessor());
}
