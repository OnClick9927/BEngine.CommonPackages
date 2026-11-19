using BEngine.Serialization;

namespace BEngine.VisualScripting;

[EditorIcon("Icons/Assets/AssetScript.png")]
[CreateAssetMenu(fileName = "New Visual Script", menuName = "Scripting/Visual Script", order = 120)]
public sealed class VisualScriptGraph : ScriptableObject
{
    public const string CurrentFormat = "BEngine.VisualScripting";
    public const int CurrentVersion = 1;

    public string format { get; set; } = CurrentFormat;
    public int version { get; set; } = CurrentVersion;
    public string initialState { get; set; } = string.Empty;
    public List<VisualScriptVariable> variables { get; set; } =
    [
        new() { name = "enabled", value = VisualScriptValue.Boolean(true) }
    ];
    public List<VisualScriptNode> nodes { get; set; } =
    [
        new() { id = "start", kind = VisualScriptNodeKind.StartEvent, position = new(20, 80) },
        new() { id = "log", kind = VisualScriptNodeKind.Log, position = new(300, 80), text = "Visual Script started" }
    ];
    public List<VisualScriptConnection> connections { get; set; } =
    [
        new() { id = "start-log", source = "start", destination = "log" }
    ];

    public static VisualScriptGraph CreateDefault(string name = "New Visual Script") => new() { name = name };

    public void Validate()
    {
        if (!string.Equals(format, CurrentFormat, StringComparison.Ordinal) || version != CurrentVersion)
            throw new InvalidDataException($"Unsupported visual script format '{format}' version {version}.");
        variables ??= [];
        nodes ??= [];
        connections ??= [];
        if (nodes.Count is < 1 or > 16384)
            throw new InvalidDataException("A visual script requires 1 to 16384 nodes.");
        if (connections.Count > 65536)
            throw new InvalidDataException("A visual script cannot contain more than 65536 connections.");

        var variableNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var variable in variables)
        {
            if (variable is null || string.IsNullOrWhiteSpace(variable.name))
                throw new InvalidDataException("Visual script variable names cannot be empty.");
            variable.name = variable.name.Trim();
            variable.value ??= VisualScriptValue.Boolean(false);
            if (!variableNames.Add(variable.name))
                throw new InvalidDataException($"Duplicate visual script variable '{variable.name}'.");
        }

        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            if (node is null || string.IsNullOrWhiteSpace(node.id) || !nodeIds.Add(node.id))
                throw new InvalidDataException("Visual script node IDs must be non-empty and unique.");
            node.value ??= VisualScriptValue.Boolean(false);
            if (!Enum.IsDefined(node.kind) || !Enum.IsDefined(node.comparison))
                throw new InvalidDataException($"Visual script node '{node.id}' has an invalid enum value.");
            if (RequiresVariable(node.kind) && !variableNames.Contains(node.variable))
                throw new InvalidDataException($"Node '{node.id}' refers to missing variable '{node.variable}'.");
            var variableKind = Matches3240()?.value.kind;
            [global::BEngine.SkipLoopGuard]
            global::BEngine.VisualScripting.VisualScriptVariable? Matches3240()
            {
                var matchSource3240 = variables;
                ArgumentNullException.ThrowIfNull(matchSource3240, "source");
                if (matchSource3240.GetType() != typeof(global::System.Collections.Generic.List<global::BEngine.VisualScripting.VisualScriptVariable>))
                {
                    foreach (var item in (global::System.Collections.Generic.IEnumerable<global::BEngine.VisualScripting.VisualScriptVariable>)matchSource3240)
                    {
                        if (item.name == node.variable)
                            return item;
                    }
            
                    return default!;
                }
            
                global::System.ReadOnlySpan<global::BEngine.VisualScripting.VisualScriptVariable> matchValues3240 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(matchSource3240);
                foreach (var item in matchValues3240)
                {
                    if (item.name == node.variable)
                        return item;
                }
            
                return default!;
            }
            if (node.kind == VisualScriptNodeKind.Branch && variableKind != VisualScriptValueKind.Boolean)
                throw new InvalidDataException($"Branch node '{node.id}' requires a Boolean variable.");
            if (node.kind is VisualScriptNodeKind.AddNumber or VisualScriptNodeKind.CompareNumber or VisualScriptNodeKind.MultiplyNumber or VisualScriptNodeKind.DeltaTime &&
                variableKind != VisualScriptValueKind.Number)
                throw new InvalidDataException($"Node '{node.id}' requires a Number variable.");
            if (node.kind == VisualScriptNodeKind.SetVariable && variableKind != node.value.kind)
                throw new InvalidDataException($"Set node '{node.id}' value type does not match variable '{node.variable}'.");
            [global::BEngine.SkipLoopGuard]
            global::BEngine.VisualScripting.VisualScriptVariable Query5444(global::System.Collections.Generic.IEnumerable<global::BEngine.VisualScripting.VisualScriptVariable> querySource5444)
            {
                ArgumentNullException.ThrowIfNull(querySource5444, "source");
                bool Matches5444(global::BEngine.VisualScripting.VisualScriptVariable item) => item.name == node.secondaryVariable;
                global::System.ReadOnlySpan<global::BEngine.VisualScripting.VisualScriptVariable> queryValues5444;
                if (querySource5444.GetType() == typeof(global::BEngine.VisualScripting.VisualScriptVariable[]))
                    queryValues5444 = (global::BEngine.VisualScripting.VisualScriptVariable[])querySource5444;
                else if (querySource5444.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.VisualScripting.VisualScriptVariable>))
                    queryValues5444 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.VisualScripting.VisualScriptVariable>)querySource5444);
                else
                {
                    foreach (var item in querySource5444)
                        if (Matches5444(item))
                            return item;
                    return global::System.Linq.Enumerable.First(global::System.Array.Empty<global::BEngine.VisualScripting.VisualScriptVariable>(), static _ => true);
                }
            
                foreach (var item in queryValues5444)
                    if (Matches5444(item))
                        return item;
                return global::System.Linq.Enumerable.First(global::System.Array.Empty<global::BEngine.VisualScripting.VisualScriptVariable>(), static _ => true);
            }
            
            if (node.kind == VisualScriptNodeKind.CompareNumber && (!variableNames.Contains(node.secondaryVariable) || Query5444(variables).value.kind != VisualScriptValueKind.Boolean))
                throw new InvalidDataException($"Compare node '{node.id}' requires a Boolean result variable.");
            if (node.kind == VisualScriptNodeKind.CustomEvent && string.IsNullOrWhiteSpace(node.text))
                throw new InvalidDataException($"Custom event node '{node.id}' requires an event name.");
            if (node.kind is VisualScriptNodeKind.StateEnterEvent or VisualScriptNodeKind.StateUpdateEvent or VisualScriptNodeKind.StateExitEvent or VisualScriptNodeKind.TransitionState && string.IsNullOrWhiteSpace(node.text))
                throw new InvalidDataException($"State node '{node.id}' requires a state name.");
            if (node.kind == VisualScriptNodeKind.CallSubGraph && string.IsNullOrWhiteSpace(node.graphPath))
                throw new InvalidDataException($"Subgraph node '{node.id}' requires a graph asset path or GUID.");
            if (node.kind == VisualScriptNodeKind.CustomAction && string.IsNullOrWhiteSpace(node.actionId))
                throw new InvalidDataException($"Custom node '{node.id}' requires a registered action ID.");
            if (node.kind == VisualScriptNodeKind.Wait && (node.value.kind != VisualScriptValueKind.Number || node.value.number < 0))
                throw new InvalidDataException($"Wait node '{node.id}' requires a nonnegative duration.");
            if (node.kind == VisualScriptNodeKind.KeyPressed && (variableKind != VisualScriptValueKind.Boolean || !Enum.TryParse<KeyCode>(node.text, out _)))
                throw new InvalidDataException($"Key node '{node.id}' requires a Boolean variable and a valid key.");
        }

        var connectionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var edge in connections)
        {
            if (edge is null || string.IsNullOrWhiteSpace(edge.id) || !connectionIds.Add(edge.id))
                throw new InvalidDataException("Visual script connection IDs must be non-empty and unique.");
            var source = Matches6318() ?? throw new InvalidDataException($"Connection '{edge.id}' has a missing source node.");
            [global::BEngine.SkipLoopGuard]
            global::BEngine.VisualScripting.VisualScriptNode? Matches6318()
            {
                var matchSource6318 = nodes;
                ArgumentNullException.ThrowIfNull(matchSource6318, "source");
                if (matchSource6318.GetType() != typeof(global::System.Collections.Generic.List<global::BEngine.VisualScripting.VisualScriptNode>))
                {
                    foreach (var item in (global::System.Collections.Generic.IEnumerable<global::BEngine.VisualScripting.VisualScriptNode>)matchSource6318)
                    {
                        if (item.id == edge.source)
                            return item;
                    }
            
                    return default!;
                }
            
                global::System.ReadOnlySpan<global::BEngine.VisualScripting.VisualScriptNode> matchValues6318 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(matchSource6318);
                foreach (var item in matchValues6318)
                {
                    if (item.id == edge.source)
                        return item;
                }
            
                return default!;
            }
            var destination = Matches6515() ?? throw new InvalidDataException($"Connection '{edge.id}' has a missing destination node.");
            [global::BEngine.SkipLoopGuard]
            global::BEngine.VisualScripting.VisualScriptNode? Matches6515()
            {
                var matchSource6515 = nodes;
                ArgumentNullException.ThrowIfNull(matchSource6515, "source");
                if (matchSource6515.GetType() != typeof(global::System.Collections.Generic.List<global::BEngine.VisualScripting.VisualScriptNode>))
                {
                    foreach (var item in (global::System.Collections.Generic.IEnumerable<global::BEngine.VisualScripting.VisualScriptNode>)matchSource6515)
                    {
                        if (item.id == edge.destination)
                            return item;
                    }
            
                    return default!;
                }
            
                global::System.ReadOnlySpan<global::BEngine.VisualScripting.VisualScriptNode> matchValues6515 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(matchSource6515);
                foreach (var item in matchValues6515)
                {
                    if (item.id == edge.destination)
                        return item;
                }
            
                return default!;
            }
            if (!OutputPorts(source.kind).Contains(edge.sourcePort, StringComparer.Ordinal))
                throw new InvalidDataException($"Connection '{edge.id}' uses invalid output '{edge.sourcePort}'.");
            if (IsEvent(destination.kind) || !string.Equals(edge.destinationPort, "In", StringComparison.Ordinal))
                throw new InvalidDataException($"Connection '{edge.id}' has an invalid destination port.");
        }
    }

    public static IReadOnlyList<string> OutputPorts(VisualScriptNodeKind kind) => kind switch
    {
        VisualScriptNodeKind.Branch => ["True", "False"],
        VisualScriptNodeKind.Stop => [],
        _ => ["Out"]
    };

    public static bool IsEvent(VisualScriptNodeKind kind) => kind is
        VisualScriptNodeKind.StartEvent or VisualScriptNodeKind.UpdateEvent or VisualScriptNodeKind.CustomEvent or
        VisualScriptNodeKind.StateEnterEvent or VisualScriptNodeKind.StateUpdateEvent or VisualScriptNodeKind.StateExitEvent;

    public static bool RequiresVariable(VisualScriptNodeKind kind) => kind is
        VisualScriptNodeKind.Branch or VisualScriptNodeKind.SetVariable or
        VisualScriptNodeKind.AddNumber or VisualScriptNodeKind.CompareNumber or VisualScriptNodeKind.MultiplyNumber or VisualScriptNodeKind.DeltaTime or VisualScriptNodeKind.KeyPressed;

    public static VisualScriptGraph Load(string path) => Document<VisualScriptGraph>.Read(path,
        static source => Restore(YamlUtility.Load<VisualScriptGraphData>(source))).ToAsset();

    internal static VisualScriptGraph Deserialize(string yaml) => Document<VisualScriptGraph>.Parse(yaml,
        static source => Restore(YamlUtility.Deserialize<VisualScriptGraphData>(source))).ToAsset();

    public void Save(string path) => Document<VisualScriptGraph>.FromAsset(this).Write(path,
        static (asset, destination) =>
        {
            asset.Validate();
            YamlUtility.Save(Capture(asset), destination);
        });

    private static VisualScriptGraph Restore(VisualScriptGraphData data)
    {
        var graph = new VisualScriptGraph
        {
            name = data.Name,
            format = data.Format,
            version = data.Version,
            initialState = data.InitialState,
            variables = ProjectItems12371(),
            nodes = data.Nodes.Select(item => new VisualScriptNode { id = item.Id, kind = item.Kind, position = new(Fix64.FromRaw(item.X), Fix64.FromRaw(item.Y)), variable = item.Variable, secondaryVariable = item.SecondaryVariable, text = item.Text, value = Restore(item.Value), comparison = item.Comparison, relative = item.Relative, breakpoint = item.Breakpoint, graphPath = item.GraphPath, actionId = item.ActionId }).ToList(),
            connections = Gather14789(data.Connections)
        };
        [global::BEngine.SkipLoopGuard]
        global::System.Collections.Generic.List<global::BEngine.VisualScripting.VisualScriptConnection> Gather14789(global::System.Collections.Generic.IEnumerable<global::BEngine.VisualScripting.VisualScriptConnectionData> items14789)
        {
            ArgumentNullException.ThrowIfNull(items14789, "source");
            global::BEngine.VisualScripting.VisualScriptConnection Evaluate14789(global::BEngine.VisualScripting.VisualScriptConnectionData item) => new VisualScriptConnection
            {
                id = item.Id,
                source = item.Source,
                sourcePort = item.SourcePort,
                destination = item.Destination,
                destinationPort = item.DestinationPort
            };
            global::System.ReadOnlySpan<global::BEngine.VisualScripting.VisualScriptConnectionData> values;
            if (items14789.GetType() == typeof(global::BEngine.VisualScripting.VisualScriptConnectionData[]))
                values = (global::BEngine.VisualScripting.VisualScriptConnectionData[])items14789;
            else if (items14789.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.VisualScripting.VisualScriptConnectionData>))
                values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.VisualScripting.VisualScriptConnectionData>)items14789);
            else
            {
                return global::System.Linq.Enumerable.ToList(global::System.Linq.Enumerable.Select(items14789, Evaluate14789));
            }

            var result = new global::System.Collections.Generic.List<global::BEngine.VisualScripting.VisualScriptConnection>(values.Length);
            global::System.Runtime.InteropServices.CollectionsMarshal.SetCount(result, values.Length);
            var destination = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(result);
            for (var index = 0; index < values.Length; index++)
                destination[index] = Evaluate14789(values[index]);
            return result;
        }
        [global::BEngine.SkipLoopGuard]
        global::System.Collections.Generic.List<global::BEngine.VisualScripting.VisualScriptVariable> ProjectItems12371()
        {
            var projectionSource12371 = data.Variables;
            ArgumentNullException.ThrowIfNull(projectionSource12371, "source");
            global::System.ReadOnlySpan<global::BEngine.VisualScripting.VisualScriptVariableData> projectionValues12371 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(projectionSource12371);
            var projectionOutput12371 = new global::System.Collections.Generic.List<global::BEngine.VisualScripting.VisualScriptVariable>(projectionValues12371.Length);
            global::System.Runtime.InteropServices.CollectionsMarshal.SetCount(projectionOutput12371, projectionValues12371.Length);
            var projectionTarget12371 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(projectionOutput12371);
            var projectionIndex12371 = 0;
            foreach (var item in projectionValues12371)
            {
                projectionTarget12371[projectionIndex12371++] = new VisualScriptVariable
                {
                    name = item.Name,
                    value = Restore(item.Value)
                };
            }
        
            return projectionOutput12371;
        }
        graph.Validate();
        return graph;
    }

    private static VisualScriptGraphData Capture(VisualScriptGraph graph) => new()
    {
        Name = graph.name,
        Format = graph.format,
        Version = graph.version,
        InitialState = graph.initialState,
        Variables = graph.variables.Select(item => new VisualScriptVariableData
        {
            Name = item.name,
            Value = Capture(item.value)
        }).ToList(),
        Nodes = graph.nodes.Select(item => new VisualScriptNodeData
        {
            Id = item.id,
            Kind = item.kind,
            X = item.position.x.RawValue,
            Y = item.position.y.RawValue,
            Variable = item.variable,
            SecondaryVariable = item.secondaryVariable,
            Text = item.text,
            Value = Capture(item.value),
            Comparison = item.comparison,
            Relative = item.relative,
            Breakpoint = item.breakpoint,
            GraphPath = item.graphPath,
            ActionId = item.actionId
        }).ToList(),
        Connections = graph.connections.Select(item => new VisualScriptConnectionData
        {
            Id = item.id,
            Source = item.source,
            SourcePort = item.sourcePort,
            Destination = item.destination,
            DestinationPort = item.destinationPort
        }).ToList()
    };

    private static VisualScriptValue Restore(VisualScriptValueData data) => new()
    {
        kind = data.Kind,
        boolean = data.Boolean,
        number = Fix64.FromRaw(data.Number),
        text = data.Text,
        vector = new(Fix64.FromRaw(data.X), Fix64.FromRaw(data.Y))
    };

    private static VisualScriptValueData Capture(VisualScriptValue value) => new()
    {
        Kind = value.kind,
        Boolean = value.boolean,
        Number = value.number.RawValue,
        Text = value.text,
        X = value.vector.x.RawValue,
        Y = value.vector.y.RawValue
    };
}

internal sealed class VisualScriptGraphData
{
    public string Format { get; set; } = VisualScriptGraph.CurrentFormat;
    public int Version { get; set; } = VisualScriptGraph.CurrentVersion;
    public string InitialState { get; set; } = string.Empty;
    public string Name { get; set; } = "New Visual Script";
    public List<VisualScriptVariableData> Variables { get; set; } = [];
    public List<VisualScriptNodeData> Nodes { get; set; } = [];
    public List<VisualScriptConnectionData> Connections { get; set; } = [];
}

internal sealed class VisualScriptVariableData
{
    public string Name { get; set; } = string.Empty;
    public VisualScriptValueData Value { get; set; } = new();
}

internal sealed class VisualScriptNodeData
{
    public string Id { get; set; } = string.Empty;
    public VisualScriptNodeKind Kind { get; set; }
    public long X { get; set; }
    public long Y { get; set; }
    public string Variable { get; set; } = string.Empty;
    public string SecondaryVariable { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public VisualScriptValueData Value { get; set; } = new();
    public VisualScriptComparison Comparison { get; set; }
    public bool Relative { get; set; }
    public bool Breakpoint { get; set; }
    public string GraphPath { get; set; } = string.Empty;
    public string ActionId { get; set; } = string.Empty;
}

internal sealed class VisualScriptConnectionData
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourcePort { get; set; } = "Out";
    public string Destination { get; set; } = string.Empty;
    public string DestinationPort { get; set; } = "In";
}

internal sealed class VisualScriptValueData
{
    public VisualScriptValueKind Kind { get; set; }
    public bool Boolean { get; set; }
    public long Number { get; set; }
    public string Text { get; set; } = string.Empty;
    public long X { get; set; }
    public long Y { get; set; }
}
