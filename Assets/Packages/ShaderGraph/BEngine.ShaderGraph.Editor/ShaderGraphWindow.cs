using BEngine.Editor;
using BEngine.Editor.UIElements;
using BEngine.Rendering.Rhi;

namespace BEngine.ShaderGraph.Editor;

[EditorWindowIcon("Icons/Assets/AssetShader.png")]
public sealed class ShaderGraphWindow : UIElementsEditorWindow<ShaderGraphWindowModel, ShaderGraphWindowController>
{
    private static readonly ShaderNodeKind[] CachedShaderNodeKindValues = Enum.GetValues<ShaderNodeKind>();
    private NodeCanvas? _canvas;
    private readonly EditorScrollView _inspectorScroll = new();
    private Vector2 _scroll;
    private ShaderGraphAsset? _presentedGraph;
    private NodePresentation[] _presentedNodes = [];
    private ConnectionPresentation[] _presentedConnections = [];
    public ShaderGraphWindow() { titleContent = new("Shader Graph"); minSize = new(680, 400); }
    protected override ShaderGraphWindowModel CreateWindowModel(EditorWindowMvcContext context) => new(context.WindowId);
    protected override ShaderGraphWindowController CreateWindowController(EditorWindowMvcContext context, ShaderGraphWindowModel model) => new(context, model);

    [MenuItem("Window/Rendering/Shader Graph", false, 235)]
    public static void Open() => GetWindow<ShaderGraphWindow>("Shader Graph").Show();
    public static void Open(ShaderGraphAsset asset)
    {
        var window = GetWindow<ShaderGraphWindow>("Shader Graph");
        window.controller.Select(asset); window.Show();
    }
    [OnOpenAsset]
    private static bool OpenAsset(string path)
    {
        if (AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(ShaderGraphAsset)) return false;
        if (AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(path) is not { } graph) return false;
        Open(graph); return true;
    }
    protected override void OnSelectionChange()
    {
        if (Selection.activeObject is ShaderGraphAsset graph) controller.Select(graph);
        Repaint();
    }
    protected override void OnEnable() { OnSelectionChange(); Undo.undoRedoPerformed += Repaint; }
    protected override void OnDisable() { _canvas?.CancelInteraction(); ClearPresentation(); Undo.undoRedoPerformed -= Repaint; }
    protected override void OnLostFocus() => _canvas?.CancelInteraction();

    private NodeCanvas Canvas
    {
        get
        {
            if (_canvas is not null) return _canvas;
            var canvas = new NodeCanvas();
            canvas.SelectionChanged += (nodes, edge) => { model.Selected = nodes.ToHashSet(StringComparer.Ordinal); model.Edge = edge; Repaint(); };
            canvas.NodeDragStarted += () =>
            {
                if (EditorAssetWritePolicy.CanWrite && model.Asset is { } graph)
                { Undo.IncrementCurrentGroup(); Undo.RecordObject(graph, "Move Shader Nodes"); }
            };
            canvas.NodesMoved += positions =>
            {
                if (!EditorAssetWritePolicy.CanWrite || model.Asset is not { } graph) return;
                foreach (var node in graph.nodes) if (positions.TryGetValue(node.id, out var position)) node.position = position;
                EditorUtility.SetDirty(graph); Repaint();
            };
            canvas.PortConnectionRequested += (source, _, target, input) => controller.Connect(source, target, input);
            canvas.NodeDoubleClicked += id =>
            {
                if (model.Asset?.nodes.FirstOrDefault(node => node.id == id)?.subGraph is { } child) controller.Enter(child);
            };
            canvas.ContextRequested += (_, _, position) => ShowCreateMenu(position);
            canvas.ViewChanged += (_, _) => Repaint();
            rootVisualElement.Add(canvas);
            return _canvas = canvas;
        }
    }

    protected override void OnGUI()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        var enabled = GUI.enabled;
        GUI.enabled = enabled && EditorAssetWritePolicy.CanWrite;
        if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(48))) controller.Select(new ShaderGraphAsset { name = "New Shader Graph" });
        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(48))) controller.Save();
        if (GUILayout.Button("Add Node", EditorStyles.toolbarButton, GUILayout.Width(84))) ShowCreateMenu(Canvas.ScreenToWorld(Canvas.viewport / 2));
        if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(58))) controller.DeleteSelection();
        GUI.enabled = enabled;
        if (model.Parents.Count > 0 && GUILayout.Button("Back", EditorStyles.toolbarButton, GUILayout.Width(45))) controller.Back();
        if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(65))) controller.Validate();
        if (GUILayout.Button("Frame", EditorStyles.toolbarButton, GUILayout.Width(55))) model.Frame = true;
        var selected = EditorGUILayout.ObjectField("", model.Asset, false, GUILayout.Width(Fix64.Max(100, GUIUtility.currentViewWidth - 405)));
        if (!ReferenceEquals(selected, model.Asset)) controller.Select(selected);
        GUILayout.EndHorizontal();
        if (model.Asset is not { isDestroyed: false } graph)
        {
            ClearPresentation();
            uiElementContainer.visible = false;
            EditorGUILayout.HelpBox("Create or select a Shader Graph. Drag an output port to an input port to connect nodes.");
            return;
        }
        var width = GUIUtility.currentViewWidth;
        var height = GUIUtility.currentViewHeight;
        var viewport = new Rect(0, 30, Fix64.Max(1, width - 260), Fix64.Max(1, height - 60));
        uiElementContainer.position = viewport; uiElementContainer.visible = true;
        var canvas = Canvas;
        canvas.allowEditing = EditorAssetWritePolicy.CanWrite;
        canvas.SetViewport(new(viewport.width, viewport.height));
        SynchronizeCanvasGraph(canvas, graph);
        canvas.SetSelection(model.Selected, model.Edge);
        if (model.Frame) { canvas.FrameAll(); model.Frame = false; }
        canvas.SetCaption("Float4 ports • drag to connect • middle mouse to pan • wheel to zoom");
        using (GUILayout.Area(new Rect(width - 254, 34, 248, Fix64.Max(1, height - 68))))
        {
            _scroll = _inspectorScroll.Begin(_scroll);
            try { DrawInspector(graph); }
            finally { _inspectorScroll.End(); }
        }
        GUI.Label(new Rect(6, height - 26, width - 12, 24), model.Message, EditorStyles.miniLabel);
        if (canvas.hasFocus && Event.current is { type: EventType.KeyDown } evt)
        {
            if (evt.keyCode == KeyCode.Delete && EditorAssetWritePolicy.CanWrite) { controller.DeleteSelection(); evt.Use(); }
            else if (evt.keyCode == KeyCode.F) { model.Frame = true; evt.Use(); }
        }
    }

    private void SynchronizeCanvasGraph(NodeCanvas canvas, ShaderGraphAsset graph)
    {
        // Assets expose mutable lists/properties. Compare the values used by the canvas,
        // rather than relying on object identity, dirty flags or a collision-prone hash.
        if (ReferenceEquals(_presentedGraph, graph) && _presentedNodes.Length == graph.nodes.Count &&
            _presentedConnections.Length == graph.connections.Count)
        {
            var unchanged = true;
            for (var index = 0; unchanged && index < _presentedNodes.Length; index++)
                unchanged = _presentedNodes[index] == NodePresentation.From(graph.nodes[index]);
            for (var index = 0; unchanged && index < _presentedConnections.Length; index++)
                unchanged = _presentedConnections[index] == ConnectionPresentation.From(graph.connections[index]);
            if (unchanged) return;
        }
        var nodes = graph.nodes.Select(NodePresentation.From).ToArray();
        var connections = graph.connections.Select(ConnectionPresentation.From).ToArray();
        var portColor = new Color32(158, 205, 242, 255);
        canvas.SetGraph(nodes.Select(node => new NodeCanvasNode(node.Id, node.Kind.ToString(), node.Position,
            new(200, 62 + Math.Max(1, ShaderGraphCompiler.Inputs(node.Kind).Length) * 22),
            node.Kind == ShaderNodeKind.Output ? new Color32(63, 106, 74, 255) : new Color32(64, 67, 75, 255),
            node.Parameter)
        {
            Inputs = ShaderGraphCompiler.Inputs(node.Kind).Select(port => new NodeCanvasPort(port, port, portColor)).ToArray(),
            Outputs = node.Kind == ShaderNodeKind.Output ? [] : [new NodeCanvasPort("Value", "Value", portColor)]
        }), connections.Select((edge, index) => new NodeCanvasConnection(index.ToString(), edge.Source, edge.Destination,
            portColor, SourcePort: "Value", DestinationPort: edge.Input)));
        _presentedGraph = graph;
        _presentedNodes = nodes;
        _presentedConnections = connections;
    }

    private void ClearPresentation()
    {
        _presentedGraph = null;
        _presentedNodes = [];
        _presentedConnections = [];
    }

    private readonly record struct NodePresentation(string Id, ShaderNodeKind Kind, Vector2 Position, string Parameter)
    {
        internal static NodePresentation From(ShaderNode node) => new(node.id, node.kind, node.position,
            node.kind == ShaderNodeKind.Parameter ? node.parameter : string.Empty);
    }

    private readonly record struct ConnectionPresentation(string Source, string Destination, string Input)
    {
        internal static ConnectionPresentation From(ShaderConnection edge) => new(edge.source, edge.destination, edge.input);
    }

    private void DrawInspector(ShaderGraphAsset graph)
    {
        using (new EditorGUI.DisabledScope(!EditorAssetWritePolicy.CanWrite))
        {
            var surface = (ShaderGraphSurface)EditorGUILayout.EnumPopup("Surface", graph.surface);
            if (surface != graph.surface) controller.Edit("Change graph surface", () => graph.surface = surface);
        }
        GUILayout.Label("Node", EditorStyles.boldLabel);
        [global::BEngine.SkipLoopGuard]
        global::BEngine.ShaderGraph.ShaderNode? Query10109(global::System.Collections.Generic.IEnumerable<global::BEngine.ShaderGraph.ShaderNode> querySource10109)
        {
            ArgumentNullException.ThrowIfNull(querySource10109, "source");
            bool Matches10109(global::BEngine.ShaderGraph.ShaderNode item) => model.Selected.Contains(item.id);
            global::System.ReadOnlySpan<global::BEngine.ShaderGraph.ShaderNode> queryValues10109;
            if (querySource10109.GetType() == typeof(global::BEngine.ShaderGraph.ShaderNode[]))
                queryValues10109 = (global::BEngine.ShaderGraph.ShaderNode[])querySource10109;
            else if (querySource10109.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.ShaderGraph.ShaderNode>))
                queryValues10109 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.ShaderGraph.ShaderNode>)querySource10109);
            else
            {
                foreach (var item in querySource10109)
                    if (Matches10109(item))
                        return item;
                return default!;
            }
        
            foreach (var item in queryValues10109)
                if (Matches10109(item))
                    return item;
            return default!;
        }
        
        var node = Query10109(graph.nodes);
        if (node is null)
        {
            GUILayout.Label("Select a node to edit its values.", EditorStyles.wordWrappedLabel);
            using (new EditorGUI.DisabledScope(!EditorAssetWritePolicy.CanWrite))
                if (GUILayout.Button("Create Material")) controller.CreateMaterial();
            return;
        }
        GUILayout.Label(node.kind.ToString());
        var enabled = GUI.enabled; GUI.enabled = enabled && EditorAssetWritePolicy.CanWrite;
        EditorGUI.BeginChangeCheck();
        var value = node.value; var number = node.number; var parameter = node.parameter;
        var texture = node.texture; var subGraph = node.subGraph; var code = node.code;
        var codeHlsl = node.codeHlsl; var codeWgsl = node.codeWgsl;
        if (node.kind is ShaderNodeKind.Color or ShaderNodeKind.Parameter or ShaderNodeKind.SubGraphInput) value = EditorGUILayout.ColorField("Value", value);
        if (node.kind == ShaderNodeKind.Scalar) number = (Fix64)EditorGUILayout.FloatField("Value", (float)number);
        if (node.kind is ShaderNodeKind.Parameter or ShaderNodeKind.TextureParameter) parameter = EditorGUILayout.TextField("Name", parameter);
        if (node.kind == ShaderNodeKind.SubGraphInput)
        {
            string[] names = ["A", "B", "C", "D"];
            parameter = names[EditorGUILayout.Popup("Input", Math.Max(0, Array.IndexOf(names, parameter)), names)];
        }
        if (node.kind == ShaderNodeKind.TextureParameter) texture = EditorGUILayout.ObjectField("Default texture", texture, false);
        if (node.kind == ShaderNodeKind.SubGraph) subGraph = EditorGUILayout.ObjectField("Subgraph", subGraph, false);
        if (node.kind == ShaderNodeKind.CustomFunction)
        {
            GUILayout.Label("GLSL body: vec4 A, B, C, D → vec4", EditorStyles.wordWrappedLabel);
            code = EditorGUILayout.TextArea(code, GUILayout.Height(110));
            GUILayout.Label("HLSL body: float4 A, B, C, D → float4", EditorStyles.wordWrappedLabel);
            codeHlsl = EditorGUILayout.TextArea(codeHlsl, GUILayout.Height(110));
            GUILayout.Label("WGSL body: A, B, C, D: vec4<f32> → vec4<f32>", EditorStyles.wordWrappedLabel);
            codeWgsl = EditorGUILayout.TextArea(codeWgsl, GUILayout.Height(110));
        }
        if (EditorGUI.EndChangeCheck()) controller.Edit("Edit Shader Node", () =>
        { node.value = value; node.number = number; node.parameter = parameter; node.texture = texture; node.subGraph = subGraph; node.code = code; node.codeHlsl = codeHlsl; node.codeWgsl = codeWgsl; });
        foreach (var port in ShaderGraphCompiler.Inputs(node.kind))
        {
            GUILayout.Label(port, EditorStyles.boldLabel);
            [global::BEngine.SkipLoopGuard]
            global::BEngine.ShaderGraph.ShaderConnection? Query12955(global::System.Collections.Generic.IEnumerable<global::BEngine.ShaderGraph.ShaderConnection> querySource12955)
            {
                ArgumentNullException.ThrowIfNull(querySource12955, "source");
                bool Matches12955(global::BEngine.ShaderGraph.ShaderConnection edge) => edge.destination == node.id && edge.input == port;
                global::System.ReadOnlySpan<global::BEngine.ShaderGraph.ShaderConnection> queryValues12955;
                if (querySource12955.GetType() == typeof(global::BEngine.ShaderGraph.ShaderConnection[]))
                    queryValues12955 = (global::BEngine.ShaderGraph.ShaderConnection[])querySource12955;
                else if (querySource12955.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.ShaderGraph.ShaderConnection>))
                    queryValues12955 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.ShaderGraph.ShaderConnection>)querySource12955);
                else
                {
                    foreach (var edge in querySource12955)
                        if (Matches12955(edge))
                            return edge;
                    return default!;
                }
            
                foreach (var edge in queryValues12955)
                    if (Matches12955(edge))
                        return edge;
                return default!;
            }
            
            var connected = Query12955(graph.connections);
            [global::BEngine.SkipLoopGuard]
            global::BEngine.ShaderGraph.ShaderNode? Query13109(global::System.Collections.Generic.IEnumerable<global::BEngine.ShaderGraph.ShaderNode> querySource13109)
            {
                ArgumentNullException.ThrowIfNull(querySource13109, "source");
                bool Matches13109(global::BEngine.ShaderGraph.ShaderNode item) => item.id == connected.source;
                global::System.ReadOnlySpan<global::BEngine.ShaderGraph.ShaderNode> queryValues13109;
                if (querySource13109.GetType() == typeof(global::BEngine.ShaderGraph.ShaderNode[]))
                    queryValues13109 = (global::BEngine.ShaderGraph.ShaderNode[])querySource13109;
                else if (querySource13109.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.ShaderGraph.ShaderNode>))
                    queryValues13109 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.ShaderGraph.ShaderNode>)querySource13109);
                else
                {
                    foreach (var item in querySource13109)
                        if (Matches13109(item))
                            return item;
                    return default!;
                }
            
                foreach (var item in queryValues13109)
                    if (Matches13109(item))
                        return item;
                return default!;
            }
            
            GUILayout.Label(connected is null ? "Default" : Query13109(graph.nodes)?.kind.ToString() ?? "Missing node");
            if (connected is not null && GUILayout.Button("Disconnect " + port)) controller.Edit("Disconnect Shader Port", () => graph.connections.Remove(connected));
        }
        GUI.enabled = enabled;
        if (node.kind == ShaderNodeKind.SubGraph && node.subGraph is { } child && GUILayout.Button("Open Subgraph")) controller.Enter(child);
        if (GUILayout.Button("GPU Node Preview")) ShaderGraphPreviewWindow.Open(graph, node.id);
        GUILayout.Space(12);
        GUILayout.Label("Materials share graph properties. Renderer overrides take precedence. GPU Node Preview compiles the selected result without changing this asset.", EditorStyles.wordWrappedLabel);
        GUILayout.Label("All ports carry four channels. Scalar nodes broadcast their value; channel nodes extract R, G, B or A.", EditorStyles.wordWrappedLabel);
        if (model.Message.Length > 0) EditorGUILayout.HelpBox(model.Message);
    }

    private void ShowCreateMenu(Vector2 position)
    {
        if (!EditorAssetWritePolicy.CanWrite || model.Asset is null) return;
        var menu = new GenericMenu();
        foreach (var kind in CachedShaderNodeKindValues)
        {
            if (kind == ShaderNodeKind.Output) continue;
            var captured = kind;
            menu.AddItem(new GUIContent(kind.ToString()), false, () => controller.Add(captured, position));
        }
        menu.ShowAsContext();
    }
}

public sealed class ShaderGraphWindowModel(string windowId) : EditorWindowModel(windowId)
{
    public ShaderGraphAsset? Asset { get; set; }
    public HashSet<string> Selected { get; set; } = new(StringComparer.Ordinal);
    public string? Edge { get; set; }
    public bool Frame { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public Stack<ShaderGraphAsset> Parents { get; } = new();
}

public sealed class ShaderGraphWindowController(EditorWindowMvcContext context, ShaderGraphWindowModel model)
    : EditorWindowController<ShaderGraphWindow, ShaderGraphWindowModel>(context, model)
{
    public void Select(ShaderGraphAsset? graph)
    { Model.Parents.Clear(); SetSelection(graph); }
    private void SetSelection(ShaderGraphAsset? graph)
    { Model.Asset = graph; Model.Selected.Clear(); Model.Edge = null; Model.Frame = true; Model.Message = string.Empty; }
    public void Enter(ShaderGraphAsset graph)
    { if (Model.Asset is { } parent) Model.Parents.Push(parent); SetSelection(graph); }
    public void Back() { if (Model.Parents.TryPop(out var parent)) SetSelection(parent); }
    public void Edit(string label, Action action)
    {
        EditorAssetWritePolicy.EnsureCanWrite(label);
        if (Model.Asset is not { } graph) return;
        Undo.IncrementCurrentGroup(); Undo.RecordObject(graph, label); action(); EditorUtility.SetDirty(graph); View.Repaint();
    }
    public void Add(ShaderNodeKind kind, Vector2 position) => Edit("Add Shader Node", () =>
    {
        var node = new ShaderNode { kind = kind, position = position,
            parameter = kind == ShaderNodeKind.SubGraphInput ? "A" : kind == ShaderNodeKind.TextureParameter ? "Texture" : "Tint" };
        Model.Asset!.nodes.Add(node); Model.Selected = [node.id]; Model.Edge = null;
    });
    public void Connect(string source, string destination, string input)
    {
        if (Model.Asset is not { } graph) return;
        EditorAssetWritePolicy.EnsureCanWrite("Connect Shader Nodes");
        var before = graph.connections;
        var candidate = before.Where(edge => edge.destination != destination || edge.input != input).ToList();
        candidate.Add(new() { source = source, destination = destination, input = input });
        graph.connections = candidate;
        try { ShaderGraphCompiler.Compile(graph, GraphicsBackend.Vulkan); }
        catch (Exception exception) { Model.Message = exception.Message; return; }
        finally { graph.connections = before; }
        Edit("Connect Shader Nodes", () => graph.connections = candidate);
        Model.Message = string.Empty;
    }
    public void DeleteSelection() => Edit("Delete Shader Selection", () =>
    {
        var graph = Model.Asset!;
        if (int.TryParse(Model.Edge, out var edge) && edge >= 0 && edge < graph.connections.Count) graph.connections.RemoveAt(edge);
        else
        {
            var removed = graph.nodes.Where(node => Model.Selected.Contains(node.id) && node.kind != ShaderNodeKind.Output).Select(node => node.id).ToHashSet(StringComparer.Ordinal);
            graph.nodes.RemoveAll(node => removed.Contains(node.id));
            graph.connections.RemoveAll(item => removed.Contains(item.source) || removed.Contains(item.destination));
        }
        Model.Selected.Clear(); Model.Edge = null;
    });
    public void Validate()
    {
        if (Model.Asset is not { } graph) return;
        try
        {
            ShaderGraphCompiler.Compile(graph, GraphicsBackend.Vulkan);
            ShaderGraphCompiler.Compile(graph, GraphicsBackend.OpenGL);
            Model.Message = "Graph structure and code generation passed. GPU Node Preview reports driver compilation errors.";
        }
        catch (Exception exception) { Model.Message = exception.Message; }
    }
    public bool Save()
    {
        if (Model.Asset is not { } graph) return false;
        try
        {
            EditorAssetWritePolicy.EnsureCanWrite("Save Shader Graph");
            ShaderGraphCompiler.Compile(graph, GraphicsBackend.Vulkan);
            if (string.IsNullOrWhiteSpace(graph.assetPath))
            {
                var folder = ProjectWindowUtil.GetActiveFolderPath() ?? "Assets";
                var path = AssetDatabase.GenerateUniqueAssetPath(folder.TrimEnd('/', '\\') + "/New Shader Graph.asset.yaml");
                AssetDatabase.CreateAsset(graph, path);
            }
            EditorUtility.SetDirty(graph); AssetDatabase.SaveAssets();
            Model.Message = "Saved " + graph.assetPath;
            return true;
        }
        catch (Exception exception) { Model.Message = exception.Message; return false; }
    }

    public ShaderGraphMaterial? CreateMaterial()
    {
        EditorAssetWritePolicy.EnsureCanWrite("Create Shader Graph Material");
        if (Model.Asset is not { } graph) return null;
        if (!Save()) return null;
        if (string.IsNullOrWhiteSpace(graph.assetPath)) return null;
        var material = new ShaderGraphMaterial { name = graph.name + " Material", graph = graph };
        var folder = ProjectWindowUtil.GetActiveFolderPath() ?? "Assets";
        var path = AssetDatabase.GenerateUniqueAssetPath(folder.TrimEnd('/', '\\') + "/New Shader Graph Material.asset.yaml");
        AssetDatabase.CreateAsset(material, path); AssetDatabase.SaveAssets();
        Model.Message = "Created " + path;
        return material;
    }
}

[CustomEditor(typeof(ShaderGraphAsset))]
public sealed class ShaderGraphAssetEditor : BAssetEditor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Shader Graph")) ShaderGraphWindow.Open((ShaderGraphAsset)target);
        GUILayout.Label("Edit nodes, connections and exposed parameters in the graph window.", EditorStyles.wordWrappedLabel);
    }
}
