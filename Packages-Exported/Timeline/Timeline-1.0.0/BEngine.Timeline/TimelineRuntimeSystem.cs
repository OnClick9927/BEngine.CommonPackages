namespace BEngine;

public sealed class TimelineRuntimeSystem : ISceneRuntimeSystem, IDisposable
{
    private ObjectPool<ControlGraphWorkspace>? _controlWorkspaces;
    private readonly List<PlayableDirector> _directors = [];
    private readonly HashSet<PlayableDirector> _tracked = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<PlayableDirector> _present = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<PlayableDirector> _controlledThisFrame = [];
    private bool _controlledInUse;
    public int order => 55;
    public string packageId => "com.bengine.timeline";

    public void Update(Scene scene, Fix64 deltaTime)
    {
        scene.GetComponents(_directors);
        try { OrderControlParentsFirst(); }
        catch
        {
            StopDirectors(_directors);
            throw;
        }
        _present.Clear();
        // Capture controlled membership at the same point as the old local
        // set, then let Tick/CollectControlled add newly acquired children.
        // A nested update must not clear the outer call's captured membership.
        var sharedControls = !_controlledInUse;
        var controlledThisFrame = sharedControls ? _controlledThisFrame : new HashSet<PlayableDirector>();
        if (sharedControls) _controlledInUse = true;
        try
        {
            controlledThisFrame.Clear();
            foreach (var director in _directors)
                if (director.isControlled) controlledThisFrame.Add(director);
            foreach (var director in _directors)
            {
                if (director.isDestroyed) continue;
                _present.Add(director);
                if (controlledThisFrame.Contains(director)) continue;
                if (director.enabled && director.gameObject.activeInHierarchy) director.Tick(deltaTime);
                else director.Suspend();
                director.CollectControlled(controlledThisFrame);
            }
            foreach (var director in _tracked)
                if (!_present.Contains(director)) director.Suspend();
            _tracked.Clear();
            // HashSet.UnionWith enumerates its IEnumerable argument through a
            // boxed enumerator, even when both sets are already reusable.
            foreach (var director in _present) _tracked.Add(director);
        }
        finally
        {
            // Do not retain a removed/failed director until another update.
            controlledThisFrame.Clear();
            if (sharedControls) _controlledInUse = false;
        }
    }

    private void OrderControlParentsFirst()
    {
        // Without a live Control clip there are no graph edges, so the current
        // scene order is already the topological order. Inspect mutable tracks
        // on each update: adding or unmuting a Control track must take effect
        // immediately, including nested assets reached through that track.
        var hasControls = false;
        foreach (var director in _directors)
        {
            if (director.playableAsset is not { isDestroyed: false } asset) continue;
            foreach (var track in asset.tracks)
                if (!track.muted && track.type == TimelineTrackType.Control && track.clips.Count > 0)
                { hasControls = true; break; }
            if (hasControls) break;
        }
        if (!hasControls) return;
        OrderControlGraph();
    }

    private void OrderControlGraph()
    {
        var pool = _controlWorkspaces ??= new(static () => new ControlGraphWorkspace(),
            onRelease: static item => item.Clear(), maxSize: 1);
        var workspace = pool.Get();
        try { OrderControlGraph(workspace); }
        finally { pool.Release(workspace); }
    }

    private void OrderControlGraph(ControlGraphWorkspace workspace)
    {
        var present = workspace.Present;
        var incoming = workspace.Incoming;
        var edges = workspace.Edges;
        var pending = workspace.Pending;
        var visited = workspace.Visited;
        foreach (var director in _directors)
        {
            present.Add(director); incoming.Add(director, 0);
            edges.Add(director, workspace.EdgeSet());
            pending.Enqueue((director, director.playableAsset));
        }
        while (pending.TryDequeue(out var input))
        {
            if (!visited.Add(input)) continue;
            foreach (var child in input.Director.ControlInputs(input.Asset))
            {
                if (!present.Contains(child.Director)) continue;
                edges[input.Director].Add(child.Director);
                pending.Enqueue(child);
            }
        }
        foreach (var children in edges.Values)
            foreach (var child in children) incoming[child]++;
        var ready = workspace.Ready;
        foreach (var director in _directors)
            if (incoming[director] == 0) ready.Enqueue(director);
        var ordered = workspace.Ordered;
        while (ready.TryDequeue(out var parent))
        {
            ordered.Add(parent);
            foreach (var child in edges[parent])
                if (--incoming[child] == 0) ready.Enqueue(child);
        }
        if (ordered.Count != _directors.Count)
        {
            throw new InvalidOperationException("Cyclic Timeline control bindings in the scene.");
        }
        _directors.Clear(); _directors.AddRange(ordered);
    }

    private sealed class ControlGraphWorkspace
    {
        internal readonly HashSet<PlayableDirector> Present = [];
        internal readonly Dictionary<PlayableDirector, int> Incoming = [];
        internal readonly Dictionary<PlayableDirector, HashSet<PlayableDirector>> Edges = [];
        internal readonly Queue<(PlayableDirector Director, TimelineAsset? Asset)> Pending = [];
        internal readonly HashSet<(PlayableDirector, TimelineAsset?)> Visited = [];
        internal readonly Queue<PlayableDirector> Ready = [];
        internal readonly List<PlayableDirector> Ordered = [];
        private readonly List<HashSet<PlayableDirector>> _edgeSets = [];
        private int _edgeCount;
        private const int MaximumRetainedCapacity = 4096;
        private const int MaximumRetainedEdgeSets = 256;
        internal HashSet<PlayableDirector> EdgeSet()
        {
            if (_edgeCount == _edgeSets.Count) _edgeSets.Add([]);
            return _edgeSets[_edgeCount++];
        }
        internal void Clear()
        {
            Present.Clear(); Incoming.Clear(); Edges.Clear(); Pending.Clear();
            Visited.Clear(); Ready.Clear(); Ordered.Clear();
            foreach (var set in _edgeSets)
            { set.Clear(); if (set.EnsureCapacity(0) > MaximumRetainedCapacity) set.TrimExcess(); }
            if (_edgeSets.Count > MaximumRetainedEdgeSets)
                _edgeSets.RemoveRange(MaximumRetainedEdgeSets, _edgeSets.Count - MaximumRetainedEdgeSets);
            if (_edgeSets.Capacity > MaximumRetainedEdgeSets * 2) _edgeSets.TrimExcess();
            _edgeCount = 0;
            if (Present.EnsureCapacity(0) > MaximumRetainedCapacity) Present.TrimExcess();
            if (Incoming.EnsureCapacity(0) > MaximumRetainedCapacity) Incoming.TrimExcess();
            if (Edges.EnsureCapacity(0) > MaximumRetainedCapacity) Edges.TrimExcess();
            if (Pending.EnsureCapacity(0) > MaximumRetainedCapacity) Pending.TrimExcess();
            if (Visited.EnsureCapacity(0) > MaximumRetainedCapacity) Visited.TrimExcess();
            if (Ready.EnsureCapacity(0) > MaximumRetainedCapacity) Ready.TrimExcess();
            if (Ordered.Capacity > MaximumRetainedCapacity) Ordered.Capacity = 0;
        }
    }

    public void Stop(Scene scene)
    {
        scene.GetComponents(_directors);
        StopDirectors(_directors.Concat(_tracked));
        _tracked.Clear();
        _present.Clear();
        _directors.Clear();
        _controlWorkspaces?.Clear();
    }

    public void Dispose() => _controlWorkspaces?.Dispose();

    private static void StopDirectors(IEnumerable<PlayableDirector> source)
    {
        var directors = (((global::System.Collections.Generic.IEnumerable<global::BEngine.PlayableDirector>)(source.Where(item => !item.isDestroyed))) switch
        {
            global::BEngine.PlayableDirector[] { Length: 0 } => global::System.Array.Empty<global::BEngine.PlayableDirector>(),
            var distinctItems8067 => distinctItems8067.ToHashSet<global::BEngine.PlayableDirector>().ToArray()});
        // Releasing a parent restores its children's previous playback. Release every root
        // first, then stop all directors so scene shutdown cannot revive a child.
        foreach (var director in directors.Where(item => !item.isControlled).ToArray()) director.Stop();
        foreach (var director in directors) director.Stop();
    }
}
