namespace BEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Animation/Playable Director")]
public sealed partial class PlayableDirector : Behaviour
{
    public PlayableDirector() => destroyed += _ =>
    {
        try { Stop(); }
        finally { _evaluationWorkspaces?.Dispose(); }
    };

    private readonly Dictionary<GameObject, AnimatorPose> _poses = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<GameObject, bool> _activationDefaults = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<ITimelineMediaPlayable> _activeMedia = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Component, ITimelineMediaPlayable> _mediaBindings = new(ReferenceEqualityComparer.Instance);
    private TimelineAsset? _effectiveAsset;
    private bool _started;
    private bool _isPlaying;
    private bool _paused;
    private bool _runtimeDriven;
    private Fix64 _time;

    [FormerlySerializedAs("assetPath")]
    public TimelineAsset? playableAsset { get; set; }
    public List<TimelineBinding> bindings { get; set; } = [];
    public bool playOnAwake { get; set; } = true;
    public bool wrapLoop { get; set; }
    public Fix64 speed { get; set; } = Fix64.One;

    [YamlDotNet.Serialization.YamlIgnore, HideInInspector]
    public Fix64 time
    {
        get => _time;
        set => _time = Mathf.Max(0, value);
    }

    [YamlDotNet.Serialization.YamlIgnore, HideInInspector]
    public bool isPlaying => _isPlaying;
    [YamlDotNet.Serialization.YamlIgnore, HideInInspector]
    public Fix64 duration => playableAsset?.duration ?? Fix64.Zero;

    public event Action<string>? signalEmitted;

    public void SetGenericBinding(string trackId, GameObject? target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackId);
        var existing = Matches1832();
        [global::BEngine.SkipLoopGuard]
        global::BEngine.TimelineBinding? Matches1832()
        {
            var matchSource1832 = bindings;
            ArgumentNullException.ThrowIfNull(matchSource1832, "source");
            if (matchSource1832.GetType() != typeof(global::System.Collections.Generic.List<global::BEngine.TimelineBinding>))
            {
                foreach (var item in (global::System.Collections.Generic.IEnumerable<global::BEngine.TimelineBinding>)matchSource1832)
                {
                    if (item.trackId == trackId)
                        return item;
                }
        
                return default!;
            }
        
            global::System.ReadOnlySpan<global::BEngine.TimelineBinding> matchValues1832 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(matchSource1832);
            foreach (var item in matchValues1832)
            {
                if (item.trackId == trackId)
                    return item;
            }
        
            return default!;
        }
        if (existing is null) bindings.Add(new TimelineBinding { trackId = trackId, target = target });
        else existing.target = target;
        ResetEvaluationState();
    }

    public GameObject? GetGenericBinding(string trackId)
    {
        foreach (var binding in bindings)
            if (binding.trackId == trackId) return binding.target;
        return null;
    }

    public void Play()
    {
        _started = true;
        _paused = false;
        _time = speed < 0 ? duration : Fix64.Zero;
        _isPlaying = playableAsset is { isDestroyed: false };
        ResetEvaluationState();
        if (_isPlaying) Evaluate();
    }

    public void Pause()
    {
        _isPlaying = false;
        _paused = true;
        Evaluate();
    }

    public void Stop()
    {
        _started = true;
        _isPlaying = false;
        _paused = false;
        _time = Fix64.Zero;
        ResetEvaluationState();
        _runtimeDriven = false;
    }

    public void Evaluate()
    {
        if (!EvaluationStack.Add(this)) throw new InvalidOperationException("Cyclic Timeline control binding.");
        try { EvaluateCore(); }
        catch { ResetEvaluationState(); throw; }
        finally { EvaluationStack.Remove(this); }
    }

    private void EvaluateCore()
    {
        if (playableAsset is not { isDestroyed: false } asset)
        { ResetEvaluationState(); return; }
        var pool = EvaluationWorkspaces;
        var workspace = pool.Get();
        try { EvaluateCore(asset, workspace); }
        finally { pool.Release(workspace); }
    }

    private void EvaluateCore(TimelineAsset asset, EvaluationWorkspace workspace)
    {
        if (!ReferenceEquals(_effectiveAsset, asset))
        {
            ResetEvaluationState();
            _effectiveAsset = asset;
        }
        foreach (var (target, pose) in _poses)
            if (!PreserveAnimatorPose(target)) pose.RestoreDefaults(false);
        EnsurePoses(asset, workspace);
        RestoreCameraDefaults();
        foreach (var (target, active) in _activationDefaults)
            if (!target.isDestroyed) target.activeSelf = active;
        var currentMedia = workspace.CurrentMedia;
        var animationSamples = workspace.AnimationSamples;
        var controls = workspace.Controls;
        var cameras = workspace.Cameras;
        foreach (var track in asset.tracks)
        {
            if (track.muted) continue;
            var target = GetGenericBinding(track.id);
            if (track.type == TimelineTrackType.Signal) continue;
            if (target is null || target.isDestroyed) continue;
            var active = workspace.ActiveClips(track, _time, asset.duration);
            if (track.type == TimelineTrackType.Activation)
            {
                target.activeSelf = active.Count > 0;
                continue;
            }
            if (track.type == TimelineTrackType.Control)
            {
                var nested = target.GetComponent<PlayableDirector>();
                if (nested?._controlOwner is { } owner && !ReferenceEquals(owner, this))
                {
                    if (active.Count == 0) continue;
                    throw new InvalidOperationException("The nested director is already controlled by another Timeline.");
                }
                if (track.controlActivation)
                {
                    _activationDefaults.TryAdd(target, target.activeSelf);
                    target.activeSelf = active.Count > 0;
                }
                if (active.Count > 0 && nested is { isDestroyed: false })
                {
                    if (!controls.TryAdd(nested, active[^1]))
                        throw new InvalidOperationException("Multiple active Control tracks target the same director.");
                }
                continue;
            }
            if (track.type == TimelineTrackType.Camera)
            {
                if (target.GetComponent<Camera2D>() is { isDestroyed: false } output)
                    cameras.Add((output, active));
                continue;
            }
            if (track.type == TimelineTrackType.Animation)
            {
                var motions = workspace.Motions; motions.Clear();
                var peers = workspace.MotionPeers; peers.Clear();
                foreach (var clip in active)
                    if (clip.ResolveClip() is { isDestroyed: false } motion)
                    { motions.Add((clip, motion)); peers.Add(clip); }
                var samples = workspace.Samples(target);
                foreach (var (clip, motion) in motions)
                {
                    var local = Mathf.Clamp((_time - clip.start) * clip.timeScale + clip.clipIn,
                        0, motion.length);
                    samples.Add((motion, local, AnimationWeight(clip, peers, _time)));
                }
                continue;
            }
            foreach (var clip in active)
            {
                var local = (Mathf.Clamp(_time - clip.start, 0, clip.duration) * clip.timeScale)
                    + clip.clipIn;
                if (track.type is TimelineTrackType.Audio or TimelineTrackType.Video &&
                         clip.ResolveMediaAsset() is { } media)
                {
                    var components = workspace.Components; components.Clear();
                    foreach (var component in target.components) components.Add(component);
                    foreach (var component in components)
                    {
                        if (component.isDestroyed || component is Behaviour { enabled: false } || !target.activeInHierarchy) continue;
                        if (!_mediaBindings.TryGetValue(component, out var playable))
                        {
                            playable = TimelineMediaAdapters.Create(component, media);
                            if (playable is null) continue;
                            _mediaBindings.Add(component, playable);
                        }
                        if (!playable.CanPlayTimelineAsset(media)) continue;
                        playable.EvaluateTimeline(media, local, _isPlaying);
                        currentMedia.Add(playable);
                    }
                }
            }
        }
        foreach (var (target, samples) in animationSamples)
            if (_poses.TryGetValue(target, out var pose)) pose.SampleWeightedClips(target, samples);
        EvaluateControls(controls, workspace.RemovedControls);
        foreach (var (output, clips) in cameras) EvaluateCamera(output, clips, workspace);
        foreach (var previous in _activeMedia)
            if (!currentMedia.Contains(previous)) previous.StopTimeline();
        foreach (var binding in _mediaBindings)
            if (!currentMedia.Contains(binding.Value)) workspace.RemovedMedia.Add(binding.Key);
        foreach (var component in workspace.RemovedMedia) _mediaBindings.Remove(component);
        _activeMedia.Clear();
        foreach (var playable in currentMedia) _activeMedia.Add(playable);
    }

    internal static Fix64 AnimationWeight(TimelineClip clip, IEnumerable<TimelineClip> neighbors, Fix64 time)
    {
        var peers = neighbors as IReadOnlyList<TimelineClip> ?? neighbors.ToArray();
        var weight = Envelope(clip, peers, time);
        for (var i = 0; i < peers.Count; i++)
        {
            var other = peers[i];
            if (other.start > clip.start && other.end < clip.end &&
                time >= other.start && time <= other.end)
                weight *= Fix64.One - Envelope(other, peers, time);
        }
        return Mathf.Clamp01(weight);
    }

    private static Fix64 Envelope(TimelineClip clip, IReadOnlyList<TimelineClip> neighbors, Fix64 time)
    {
        var (easeIn, easeOut) = BlendDurations(clip, neighbors);
        var weight = Fix64.One;
        if (easeIn > 0) weight = Fix64.Min(weight, (time - clip.start) / easeIn);
        if (easeOut > 0) weight = Fix64.Min(weight, (clip.end - time) / easeOut);
        return Mathf.Clamp01(weight);
    }

    internal static (Fix64 In, Fix64 Out) BlendDurations(TimelineClip clip,
        IEnumerable<TimelineClip> neighbors)
    {
        if (neighbors is IReadOnlyList<TimelineClip> list) return BlendDurations(clip, list);
        var easeIn = Fix64.Zero;
        var easeOut = Fix64.Zero;
        foreach (var other in neighbors)
        {
            if (ReferenceEquals(clip, other)) continue;
            if (other.start < clip.start && other.end > clip.start && other.end <= clip.end)
                easeIn = Fix64.Max(easeIn, other.end - clip.start);
            else if (other.start > clip.start && other.start < clip.end && other.end >= clip.end)
                easeOut = Fix64.Max(easeOut, clip.end - other.start);
            else if (other.start < clip.start && other.end > clip.end)
            {
                easeIn = Fix64.Max(easeIn, clip.duration / 2);
                easeOut = Fix64.Max(easeOut, clip.duration / 2);
            }
        }
        var duration = Fix64.Max(0, clip.duration);
        return (Fix64.Min(duration, clip.easeInDuration > 0 ? clip.easeInDuration : easeIn),
            Fix64.Min(duration, clip.easeOutDuration > 0 ? clip.easeOutDuration : easeOut));
    }

    private static (Fix64 In, Fix64 Out) BlendDurations(TimelineClip clip, IReadOnlyList<TimelineClip> neighbors)
    {
        var easeIn = Fix64.Zero;
        var easeOut = Fix64.Zero;
        for (var i = 0; i < neighbors.Count; i++)
        {
            var other = neighbors[i];
            if (ReferenceEquals(clip, other)) continue;
            if (other.start < clip.start && other.end > clip.start && other.end <= clip.end)
                easeIn = Fix64.Max(easeIn, other.end - clip.start);
            else if (other.start > clip.start && other.start < clip.end && other.end >= clip.end)
                easeOut = Fix64.Max(easeOut, clip.end - other.start);
            else if (other.start < clip.start && other.end > clip.end)
            {
                easeIn = Fix64.Max(easeIn, clip.duration / 2);
                easeOut = Fix64.Max(easeOut, clip.duration / 2);
            }
        }
        var duration = Fix64.Max(0, clip.duration);
        return (Fix64.Min(duration, clip.easeInDuration > 0 ? clip.easeInDuration : easeIn),
            Fix64.Min(duration, clip.easeOutDuration > 0 ? clip.easeOutDuration : easeOut));
    }

    internal void Tick(Fix64 deltaTime)
    {
        if (_controlOwner is not null) return;
        _runtimeDriven = true;
        if (!_started && playOnAwake) Play();
        if (!_isPlaying) { if (_paused) Evaluate(); return; }
        var asset = playableAsset;
        if (asset is null || asset.isDestroyed) { Stop(); return; }
        var length = asset.duration;
        if (length <= 0) { _isPlaying = false; Evaluate(); return; }
        var previous = _time;
        var advance = Fix64.Max(0, deltaTime) * speed;
        var elapsed = previous + advance;
        _time = wrapLoop ? Mathf.Repeat(elapsed, length) : Fix64.Clamp(elapsed, 0, length);
        if (!wrapLoop && (advance > 0 && elapsed >= length || advance < 0 && elapsed <= 0)) _isPlaying = false;
        Evaluate();
        DispatchSignalRange(asset, previous, elapsed, length, wrapLoop, []);
    }

    internal void Suspend()
    {
        if (_controlOwner is not null) return;
        if (!_isPlaying && _poses.Count == 0 && _activeMedia.Count == 0 && _controlled.Count == 0 &&
            _cameraDefaults.Count == 0 && _activationDefaults.Count == 0) return;
        _isPlaying = false;
        _paused = false;
        _started = false;
        _time = Fix64.Zero;
        ResetEvaluationState();
        _runtimeDriven = false;
    }

    private bool PreserveAnimatorPose(GameObject target) => _runtimeDriven &&
        target.activeInHierarchy && target.GetComponent<Animator>() is { enabled: true };

    private void EnsurePoses(TimelineAsset asset, EvaluationWorkspace workspace)
    {
        foreach (var binding in bindings)
            if (binding.target is { isDestroyed: false } target && workspace.TargetSet.Add(target))
                workspace.Targets.Add(target);
        foreach (var target in workspace.Targets)
        {
            if (!_poses.TryGetValue(target, out var pose))
                _poses.Add(target, pose = new AnimatorPose());
            var clips = workspace.RegistrationClips; clips.Clear();
            var clipSet = workspace.RegistrationClipSet; clipSet.Clear();
            var activates = false;
            foreach (var track in asset.tracks)
            {
                if (!ReferenceEquals(GetGenericBinding(track.id), target)) continue;
                if (track.type == TimelineTrackType.Activation) activates = true;
                if (track.type != TimelineTrackType.Animation || track.muted) continue;
                foreach (var clip in track.clips)
                    if (clip.ResolveClip() is { } motion && clipSet.Add(motion)) clips.Add(motion);
            }
            pose.Register(target, clips);
            if (activates) _activationDefaults.TryAdd(target, target.activeSelf);
        }
    }

    private void DispatchSignals(TimelineAsset asset, Fix64 previous, Fix64 elapsed, Fix64 length, bool loop)
    {
        if (signalEmitted is null || elapsed == previous || length <= 0) return;
        var reverse = elapsed < previous;
        var lower = Fix64.Min(previous, elapsed); var upper = Fix64.Max(previous, elapsed);
        var firstCycle = loop ? Mathf.FloorToInt(lower / length) : 0;
        var lastCycle = loop ? Mathf.FloorToInt(upper / length) : 0;
        if ((long)lastCycle - firstCycle > 10000) throw new InvalidOperationException("Timeline signal step crosses too many cycles.");
        // Keep each invocation's snapshot independent: callbacks can mutate the
        // asset, replace handlers, or dispatch another range on this director.
        List<SignalMarker>? signals = null;
        for (long cycle = firstCycle; cycle <= lastCycle; cycle++)
        {
            foreach (var track in asset.tracks)
            {
                if (track.type != TimelineTrackType.Signal || track.muted) continue;
                foreach (var clip in track.clips)
                {
                    if (string.IsNullOrWhiteSpace(clip.signalName)) continue;
                    var marker = clip.start + (Fix64)cycle * length;
                    if (reverse ? marker >= elapsed && marker < previous : marker > previous && marker <= elapsed)
                    {
                        signals ??= new List<SignalMarker>();
                        signals.Add(new SignalMarker(marker, clip.signalName, signals.Count));
                    }
                }
            }
        }
        if (signals is null) return;
        // List.Sort itself is unstable; the collection ordinal makes equal-time
        // markers retain the same cycle/track/clip order as the old stable OrderBy.
        if (signals.Count > 1) signals.Sort(reverse ? SignalDescending : SignalAscending);
        foreach (var signal in signals)
            signalEmitted?.Invoke(signal.Name);
    }

    private readonly struct SignalMarker(Fix64 time, string name, int order)
    {
        internal readonly Fix64 Time = time;
        internal readonly string Name = name;
        internal readonly int Order = order;
    }

    private static readonly Comparison<SignalMarker> SignalAscending = CompareSignalsAscending;
    private static readonly Comparison<SignalMarker> SignalDescending = CompareSignalsDescending;

    private static int CompareSignalsAscending(SignalMarker left, SignalMarker right)
    {
        var order = left.Time.CompareTo(right.Time);
        return order != 0 ? order : left.Order.CompareTo(right.Order);
    }

    private static int CompareSignalsDescending(SignalMarker left, SignalMarker right)
    {
        var order = right.Time.CompareTo(left.Time);
        return order != 0 ? order : left.Order.CompareTo(right.Order);
    }

    private void ResetEvaluationState()
    {
        ReleaseControls();
        RestoreCameraDefaults();
        _cameraDefaults.Clear();
        foreach (var (target, pose) in _poses)
            if (!PreserveAnimatorPose(target)) pose.RestoreDefaults(false);
        _poses.Clear();
        foreach (var (target, active) in _activationDefaults)
            if (!target.isDestroyed) target.activeSelf = active;
        _activationDefaults.Clear();
        foreach (var playable in _activeMedia.Concat(_mediaBindings.Values).Distinct()) playable.StopTimeline();
        _activeMedia.Clear();
        _mediaBindings.Clear();
        _effectiveAsset = null;
    }
}
