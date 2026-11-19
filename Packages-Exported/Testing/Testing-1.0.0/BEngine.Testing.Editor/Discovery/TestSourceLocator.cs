using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace BEngine.Testing.Editor;

public static class TestSourceLocator
{
    public static void Populate(IEnumerable<TestCase> tests)
    {
        var maps = new Dictionary<string, PdbSourceMap?>(StringComparer.Ordinal);
        try
        {
            foreach (var test in tests)
            {
                var path = test.Method.Module.Assembly.Location;
                if (string.IsNullOrWhiteSpace(path)) continue;
                if (!maps.TryGetValue(path, out var map))
                {
                    map = PdbSourceMap.Open(Path.ChangeExtension(path, ".pdb"));
                    maps.Add(path, map);
                }
                if (map is not null && map.TryLocate(test.Method, out var file, out var line))
                {
                    test.SourceFile = file;
                    test.SourceLine = line;
                }
            }
        }
        finally { foreach (var map in maps.Values) map?.Dispose(); }
    }

    public static bool TryLocate(MethodInfo method, out string file, out int line)
    {
        file = string.Empty;
        line = 0;
        var assemblyPath = method.Module.Assembly.Location;
        if (string.IsNullOrWhiteSpace(assemblyPath)) return false;
        var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
        if (!File.Exists(pdbPath)) return false;
        using var map = PdbSourceMap.Open(pdbPath);
        return map is not null && map.TryLocate(method, out file, out line);
    }

    // Scoped to one discovery call: share readers and async lookup work without
    // retaining loaded assemblies or stale PDB contents across refresh/unload.
    private sealed class PdbSourceMap(MetadataReaderProvider provider) : IDisposable
    {
        private readonly MetadataReader _reader = provider.GetMetadataReader();
        private Dictionary<MethodDefinitionHandle, (string File, int Line)>? _stateMachines;

        internal static PdbSourceMap? Open(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var provider = MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(path));
                try { return new PdbSourceMap(provider); }
                catch { provider.Dispose(); throw; }
            }
            catch (Exception error) when (error is IOException or BadImageFormatException or ArgumentException)
            { return null; }
        }

        internal bool TryLocate(MethodInfo method, out string file, out int line)
        {
            file = string.Empty; line = 0;
            try
            {
                var handle = MetadataTokens.MethodDefinitionHandle(method.MetadataToken & 0x00ffffff);
                if (TryRead(_reader, _reader.GetMethodDebugInformation(handle), out file, out line)) return true;
                if (_stateMachines is null)
                {
                    var locations = new Dictionary<MethodDefinitionHandle, (string File, int Line)>();
                    foreach (var debugHandle in _reader.MethodDebugInformation)
                    {
                        var information = _reader.GetMethodDebugInformation(debugHandle);
                        var kickoff = information.GetStateMachineKickoffMethod();
                        if (!kickoff.IsNil && TryRead(_reader, information, out var source, out var sourceLine))
                            locations.TryAdd(kickoff, (source, sourceLine));
                    }
                    _stateMachines = locations;
                }
                if (_stateMachines.TryGetValue(handle, out var location))
                { file = location.File; line = location.Line; return true; }
            }
            catch (Exception error) when (error is IOException or BadImageFormatException or ArgumentException)
            { }
            return false;
        }

        public void Dispose() => provider.Dispose();
    }

    private static bool TryRead(
        MetadataReader reader,
        MethodDebugInformation information,
        out string file,
        out int line)
    {
        foreach (var point in information.GetSequencePoints())
        {
            if (point.IsHidden) continue;
            var documentHandle = point.Document.IsNil ? information.Document : point.Document;
            if (documentHandle.IsNil) continue;
            file = reader.GetString(reader.GetDocument(documentHandle).Name);
            line = point.StartLine;
            return line > 0;
        }
        file = string.Empty;
        line = 0;
        return false;
    }
}
