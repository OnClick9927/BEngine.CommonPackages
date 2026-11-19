namespace BEngine;

public sealed partial class PlayableDirector
{
    private ObjectPool<EvaluationWorkspace>? _evaluationWorkspaces;
    private ObjectPool<EvaluationWorkspace> EvaluationWorkspaces => _evaluationWorkspaces ??= new(
        static () => new EvaluationWorkspace(), onRelease: static item => item.Clear(), maxSize: 1);

    // Each evaluation owns its buffers through media/control/camera consumption. Directors
    // never share mutable scratch, including when evaluating a nested Timeline.
    private sealed class EvaluationWorkspace
    {
        internal readonly HashSet<ITimelineMediaPlayable> CurrentMedia = new(ReferenceEqualityComparer.Instance);
        internal readonly Dictionary<GameObject, List<(AnimationClip Clip, Fix64 Time, Fix64 Weight)>> AnimationSamples = new(ReferenceEqualityComparer.Instance);
        internal readonly Dictionary<PlayableDirector, TimelineClip> Controls = new(ReferenceEqualityComparer.Instance);
        internal readonly List<(Camera2D Output, List<TimelineClip> Clips)> Cameras = [];
        internal readonly List<(TimelineClip Clip, AnimationClip Motion)> Motions = [];
        internal readonly List<TimelineClip> MotionPeers = [];
        internal readonly List<Component> Components = [];
        internal readonly List<Component> RemovedMedia = [];
        internal readonly List<PlayableDirector> RemovedControls = [];
        internal readonly List<(TimelineClip Clip, Camera2D Source)> Shots = [];
        internal readonly List<TimelineClip> ShotPeers = [];
        internal readonly List<Fix64> ShotWeights = [];
        internal readonly List<GameObject> Targets = [];
        internal readonly HashSet<GameObject> TargetSet = new(ReferenceEqualityComparer.Instance);
        internal readonly List<AnimationClip> RegistrationClips = [];
        internal readonly HashSet<AnimationClip> RegistrationClipSet = new(ReferenceEqualityComparer.Instance);
        private readonly List<List<TimelineClip>> _clipLists = [];
        private readonly List<(TimelineClip Clip, int Order)> _clipOrder = [];
        private readonly List<List<(AnimationClip Clip, Fix64 Time, Fix64 Weight)>> _sampleLists = [];
        private int _clipCount, _sampleCount;
        private const int MaximumRetainedCapacity = 4096;
        private const int MaximumRetainedLists = 64;

        internal List<TimelineClip> ActiveClips(TimelineTrack track, Fix64 time, Fix64 duration)
        {
            if (_clipCount == _clipLists.Count) _clipLists.Add([]);
            var result = _clipLists[_clipCount++];
            _clipOrder.Clear();
            foreach (var clip in track.clips)
                if (clip.duration > 0 && time >= clip.start &&
                    (time < clip.end || time == duration && time == clip.end))
                    _clipOrder.Add((clip, _clipOrder.Count));
            // The authored ordinal preserves OrderBy's stable tie ordering while
            // List.Sort also keeps large overlapping tracks O(n log n).
            if (_clipOrder.Count > 1) _clipOrder.Sort(CompareClips);
            foreach (var item in _clipOrder) result.Add(item.Clip);
            return result;
        }

        private static int CompareClips((TimelineClip Clip, int Order) left, (TimelineClip Clip, int Order) right)
        {
            var order = left.Clip.start.CompareTo(right.Clip.start);
            return order != 0 ? order : left.Order.CompareTo(right.Order);
        }

        internal List<(AnimationClip Clip, Fix64 Time, Fix64 Weight)> Samples(GameObject target)
        {
            if (AnimationSamples.TryGetValue(target, out var result)) return result;
            if (_sampleCount == _sampleLists.Count) _sampleLists.Add([]);
            result = _sampleLists[_sampleCount++];
            AnimationSamples.Add(target, result);
            return result;
        }

        internal void Clear()
        {
            ClearSet(CurrentMedia); ClearMap(AnimationSamples); ClearMap(Controls);
            ClearList(Cameras); ClearList(Motions); ClearList(MotionPeers);
            ClearList(Components); ClearList(RemovedMedia); ClearList(RemovedControls);
            ClearList(Shots); ClearList(ShotPeers); ClearList(ShotWeights);
            ClearList(Targets); ClearSet(TargetSet);
            ClearList(RegistrationClips); ClearSet(RegistrationClipSet);
            ClearList(_clipOrder);
            foreach (var list in _clipLists) ClearList(list);
            foreach (var list in _sampleLists) ClearList(list);
            if (_clipLists.Count > MaximumRetainedLists) _clipLists.RemoveRange(MaximumRetainedLists, _clipLists.Count - MaximumRetainedLists);
            if (_sampleLists.Count > MaximumRetainedLists) _sampleLists.RemoveRange(MaximumRetainedLists, _sampleLists.Count - MaximumRetainedLists);
            if (_clipLists.Capacity > MaximumRetainedLists * 2) _clipLists.TrimExcess();
            if (_sampleLists.Capacity > MaximumRetainedLists * 2) _sampleLists.TrimExcess();
            _clipCount = _sampleCount = 0;
        }

        private static void ClearList<T>(List<T> list)
        { list.Clear(); if (list.Capacity > MaximumRetainedCapacity) list.Capacity = 0; }
        private static void ClearSet<T>(HashSet<T> set)
        { set.Clear(); if (set.EnsureCapacity(0) > MaximumRetainedCapacity) set.TrimExcess(); }
        private static void ClearMap<TKey, TValue>(Dictionary<TKey, TValue> map) where TKey : notnull
        { map.Clear(); if (map.EnsureCapacity(0) > MaximumRetainedCapacity) map.TrimExcess(); }
    }
}
