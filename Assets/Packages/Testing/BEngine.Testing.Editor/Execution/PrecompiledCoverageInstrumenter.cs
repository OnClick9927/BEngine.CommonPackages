using Mono.Cecil;
using Mono.Cecil.Cil;

namespace BEngine.Testing.Editor;

/// <summary>Instruments an unsigned assembly copy using portable PDB sequence points. Originals are never modified.</summary>
public static class PrecompiledCoverageInstrumenter
{
    public static int Instrument(string assemblyPath, string outputPath)
    {
        assemblyPath = Path.GetFullPath(assemblyPath); outputPath = Path.GetFullPath(outputPath);
        if (assemblyPath.Equals(outputPath, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Choose an output copy for coverage.");
        using var resolver = new DefaultAssemblyResolver(); resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));
        resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(CodeCoverage).Assembly.Location)); resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(object).Assembly.Location));
        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { ReadSymbols = true, InMemory = true, AssemblyResolver = resolver });
        if (assembly.Name.HasPublicKey) throw new NotSupportedException("Coverage of signed assemblies requires re-signing with the publisher's key.");
        var module = assembly.MainModule;
        if (Gather1268(module.AssemblyReferences) && assembly.Name.Name.StartsWith("BEngine.Testing", StringComparison.Ordinal))
            throw new InvalidOperationException("Do not instrument the coverage framework itself.");
        [global::BEngine.SkipLoopGuard]
        bool Gather1268(global::System.Collections.Generic.IEnumerable<global::Mono.Cecil.AssemblyNameReference> items1268)
        {
            ArgumentNullException.ThrowIfNull(items1268, "source");
            bool Evaluate1268(global::Mono.Cecil.AssemblyNameReference reference) => reference.Name == typeof(CodeCoverage).Assembly.GetName().Name;
            global::System.ReadOnlySpan<global::Mono.Cecil.AssemblyNameReference> values;
            if (items1268.GetType() == typeof(global::Mono.Cecil.AssemblyNameReference[]))
                values = (global::Mono.Cecil.AssemblyNameReference[])items1268;
            else if (items1268.GetType() == typeof(global::System.Collections.Generic.List<global::Mono.Cecil.AssemblyNameReference>))
                values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::Mono.Cecil.AssemblyNameReference>)items1268);
            else
                return global::System.Linq.Enumerable.Any(items1268, Evaluate1268);
            foreach (var reference in values)
                if (Evaluate1268(reference))
                    return true;
            return false;
        }
        var hit = module.ImportReference(typeof(CodeCoverage).GetMethod(nameof(CodeCoverage.Hit))!);
        var hitBranch = module.ImportReference(typeof(CodeCoverage).GetMethod(nameof(CodeCoverage.HitBranch))!);
        var sites = new List<(string File, int Line)>();
        var branches = new List<(string File, int Line, int Block, int Outcomes)>();
        foreach (var type in Types(module.Types)) {
                                                      var filteredSource1961 = type.Methods;
                                                      ArgumentNullException.ThrowIfNull(filteredSource1961, "source");
                                                      foreach (var method in filteredSource1961)
                                                      {
                                                          if (!(method.HasBody))
                                                              continue;
                                                          var points = method.DebugInformation.SequencePoints.Where(point => !point.IsHidden && point.StartLine > 0).GroupBy(point => point.Offset).ToDictionary(group => group.Key, group => group.First());
                                                          var instructions = method.Body.Instructions.ToArray();
                                                          var il = method.Body.GetILProcessor();
                                                          // Short relative branches may become too distant after probes are inserted.
                                                          foreach (var instruction in instructions)
                                                              ExpandBranch(instruction);
                                                          foreach (var original in instructions)
                                                          {
                                                              if (!points.TryGetValue(original.Offset, out var point))
                                                                  continue;
                                                              var first = il.Create(OpCodes.Ldstr, point.Document.Url);
                                                              Redirect(method, original, first);
                                                              il.InsertBefore(original, first);
                                                              il.InsertBefore(original, il.Create(OpCodes.Ldc_I4, point.StartLine));
                                                              il.InsertBefore(original, il.Create(OpCodes.Call, hit));
                                                              sites.Add((point.Document.Url, point.StartLine));
                                                          }
                                                  
                                                          {
                                                              var __filteredSource3084 = instructions;
                                                              ArgumentNullException.ThrowIfNull(__filteredSource3084, "source");
                                                              foreach (var original in __filteredSource3084)
                                                              {
                                                                  if (!(original.OpCode.FlowControl == FlowControl.Cond_Branch))
                                                                      continue;
                                                                  var point = points.Values.Where(point => point.Offset <= original.Offset).OrderByDescending(point => point.Offset).FirstOrDefault();
                                                                  if (point is null || original.Next is null)
                                                                      continue;
                                                                  var next = original.Next;
                                                                  var targets = original.Operand is Instruction[] cases ? cases : original.Operand is Instruction target ? new[]
                                                                  {
                                                                      target
                                                                  }
                                                  
                                                                  : [];
                                                                  if (targets.Length == 0)
                                                                      continue;
                                                                  var block = branches.Count;
                                                                  branches.Add((point.Document.Url, point.StartLine, block, targets.Length + 1));
                                                                  var taken = new Instruction[targets.Length];
                                                                  for (var outcome = 0; outcome <= targets.Length; outcome++)
                                                                  {
                                                                      var first = il.Create(OpCodes.Ldstr, point.Document.Url);
                                                                      il.InsertBefore(next, first);
                                                                      il.InsertBefore(next, il.Create(OpCodes.Ldc_I4, point.StartLine));
                                                                      il.InsertBefore(next, il.Create(OpCodes.Ldc_I4, block));
                                                                      il.InsertBefore(next, il.Create(OpCodes.Ldc_I4, outcome));
                                                                      il.InsertBefore(next, il.Create(OpCodes.Call, hitBranch));
                                                                      il.InsertBefore(next, il.Create(OpCodes.Br, outcome == 0 ? next : targets[outcome - 1]));
                                                                      if (outcome > 0)
                                                                          taken[outcome - 1] = first;
                                                                  }
                                                  
                                                                  original.Operand = original.Operand is Instruction[] ? taken : taken[0];
                                                              }
                                                          }
                                                  
                                                          method.Body.MaxStackSize += 4;
                                                      }
                                                  }
        if (sites.Count == 0) throw new InvalidDataException("Assembly has no executable portable PDB sequence points.");
        var moduleType = Gather8669(module.Types);
        [global::BEngine.SkipLoopGuard]
        global::Mono.Cecil.TypeDefinition Gather8669(global::System.Collections.Generic.IEnumerable<global::Mono.Cecil.TypeDefinition> items8669)
        {
            ArgumentNullException.ThrowIfNull(items8669, "source");
            bool Evaluate8669(global::Mono.Cecil.TypeDefinition type) => type.Name == "<Module>";
            global::System.ReadOnlySpan<global::Mono.Cecil.TypeDefinition> values;
            if (items8669.GetType() == typeof(global::Mono.Cecil.TypeDefinition[]))
                values = (global::Mono.Cecil.TypeDefinition[])items8669;
            else if (items8669.GetType() == typeof(global::System.Collections.Generic.List<global::Mono.Cecil.TypeDefinition>))
                values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::Mono.Cecil.TypeDefinition>)items8669);
            else
                return global::System.Linq.Enumerable.Single(items8669, Evaluate8669);
            var found = false;
            global::Mono.Cecil.TypeDefinition result = default!;
            foreach (var type in values)
            {
                if (!Evaluate8669(type))
                    continue;
                if (found)
                    return global::System.Linq.Enumerable.Single(new global::Mono.Cecil.TypeDefinition[2], static _ => true);
                result = type;
                found = true;
            }

            if (found)
                return result;
            return global::System.Linq.Enumerable.Single(global::System.Array.Empty<global::Mono.Cecil.TypeDefinition>(), static _ => true);
        }
        var initializer = Gather8749(moduleType.Methods);
        [global::BEngine.SkipLoopGuard]
        global::Mono.Cecil.MethodDefinition? Gather8749(global::System.Collections.Generic.IEnumerable<global::Mono.Cecil.MethodDefinition> items8749)
        {
            ArgumentNullException.ThrowIfNull(items8749, "source");
            bool Evaluate8749(global::Mono.Cecil.MethodDefinition method) => method.Name == ".cctor";
            global::System.ReadOnlySpan<global::Mono.Cecil.MethodDefinition> values;
            if (items8749.GetType() == typeof(global::Mono.Cecil.MethodDefinition[]))
                values = (global::Mono.Cecil.MethodDefinition[])items8749;
            else if (items8749.GetType() == typeof(global::System.Collections.Generic.List<global::Mono.Cecil.MethodDefinition>))
                values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::Mono.Cecil.MethodDefinition>)items8749);
            else
                return global::System.Linq.Enumerable.FirstOrDefault(items8749, Evaluate8749);
            foreach (var method in values)
                if (Evaluate8749(method))
                    return method;
            return default!;
        }
        if (initializer is null)
        {
            initializer = new(".cctor", Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName, module.TypeSystem.Void);
            moduleType.Methods.Add(initializer); initializer.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ret));
        }
        var register = module.ImportReference(typeof(CodeCoverage).GetMethod(nameof(CodeCoverage.Register))!); var init = initializer.Body.GetILProcessor(); var originalStart = initializer.Body.Instructions[0];
        foreach (var group in sites.Distinct().GroupBy(site => site.File))
        {
            var lines = (group.Select(site => site.Line) switch { int[] { Length: 0 } => global::System.Array.Empty<int>(), var distinctItems9541 => distinctItems9541.ToHashSet().ToArray() });
            init.InsertBefore(originalStart, init.Create(OpCodes.Ldstr, group.Key)); init.InsertBefore(originalStart, init.Create(OpCodes.Ldc_I4, lines.Length));
            init.InsertBefore(originalStart, init.Create(OpCodes.Newarr, module.TypeSystem.Int32));
            for (var i = 0; i < lines.Length; i++)
            { init.InsertBefore(originalStart, init.Create(OpCodes.Dup)); init.InsertBefore(originalStart, init.Create(OpCodes.Ldc_I4, i)); init.InsertBefore(originalStart, init.Create(OpCodes.Ldc_I4, lines[i])); init.InsertBefore(originalStart, init.Create(OpCodes.Stelem_I4)); }
            init.InsertBefore(originalStart, init.Create(OpCodes.Call, register));
        }
        var registerBranch = module.ImportReference(typeof(CodeCoverage).GetMethod(nameof(CodeCoverage.RegisterBranch))!);
        foreach (var branch in branches)
        {
            init.InsertBefore(originalStart, init.Create(OpCodes.Ldstr, branch.File)); init.InsertBefore(originalStart, init.Create(OpCodes.Ldc_I4, branch.Line));
            init.InsertBefore(originalStart, init.Create(OpCodes.Ldc_I4, branch.Block)); init.InsertBefore(originalStart, init.Create(OpCodes.Ldc_I4, branch.Outcomes));
            init.InsertBefore(originalStart, init.Create(OpCodes.Call, registerBranch));
        }
        initializer.Body.MaxStackSize = Math.Max(initializer.Body.MaxStackSize, 6);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!); assembly.Write(outputPath, new WriterParameters { WriteSymbols = true });
        return sites.Count;
    }
    private static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> types)
    { foreach (var type in types) { yield return type; foreach (var nested in Types(type.NestedTypes)) yield return nested; } }
    private static void Redirect(MethodDefinition method, Instruction original, Instruction replacement)
    {
        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.Operand == original) instruction.Operand = replacement;
            else if (instruction.Operand is Instruction[] targets) for (var i = 0; i < targets.Length; i++) if (targets[i] == original) targets[i] = replacement;
        }
        foreach (var handler in method.Body.ExceptionHandlers)
        {
            if (handler.TryStart == original) handler.TryStart = replacement; if (handler.TryEnd == original) handler.TryEnd = replacement;
            if (handler.HandlerStart == original) handler.HandlerStart = replacement; if (handler.HandlerEnd == original) handler.HandlerEnd = replacement;
            if (handler.FilterStart == original) handler.FilterStart = replacement;
        }
    }
    private static void ExpandBranch(Instruction instruction)
    {
        instruction.OpCode = instruction.OpCode.Code switch
        {
            Code.Br_S => OpCodes.Br, Code.Brfalse_S => OpCodes.Brfalse, Code.Brtrue_S => OpCodes.Brtrue, Code.Beq_S => OpCodes.Beq,
            Code.Bge_S => OpCodes.Bge, Code.Bge_Un_S => OpCodes.Bge_Un, Code.Bgt_S => OpCodes.Bgt, Code.Bgt_Un_S => OpCodes.Bgt_Un,
            Code.Ble_S => OpCodes.Ble, Code.Ble_Un_S => OpCodes.Ble_Un, Code.Blt_S => OpCodes.Blt, Code.Blt_Un_S => OpCodes.Blt_Un,
            Code.Bne_Un_S => OpCodes.Bne_Un, Code.Leave_S => OpCodes.Leave, _ => instruction.OpCode
        };
    }
}
