namespace BEngine;

public sealed partial class PlayableDirector
{
    private readonly Dictionary<Camera2D, CameraState> _cameraDefaults = new(ReferenceEqualityComparer.Instance);

    private void RestoreCameraDefaults()
    {
        foreach (var (camera, state) in _cameraDefaults)
            if (!camera.isDestroyed && !camera.gameObject.isDestroyed) state.Restore(camera);
    }

    private void EvaluateCamera(Camera2D output, List<TimelineClip> clips, EvaluationWorkspace workspace)
    {
        var shots = workspace.Shots; shots.Clear();
        var peers = workspace.ShotPeers; peers.Clear();
        var weights = workspace.ShotWeights; weights.Clear();
        foreach (var clip in clips)
            if (GetGenericBinding(clip.cameraBinding)?.GetComponent<Camera2D>() is { isDestroyed: false } source &&
                !ReferenceEquals(source, output)) { shots.Add((clip, source)); peers.Add(clip); }
        if (shots.Count == 0) return;
        if (!_cameraDefaults.ContainsKey(output)) _cameraDefaults.Add(output, CameraState.Capture(output));
        var sum = Fix64.Zero;
        foreach (var shot in shots)
        { var weight = AnimationWeight(shot.Clip, peers, _time); weights.Add(weight); sum += weight; }
        if (sum <= 0) { weights[^1] = 1; sum = 1; }
        var position = Vector2.zero; var rotation = Fix64.Zero; var size = Fix64.Zero;
        var color = new Color(0, 0, 0, 0);
        var referenceAngle = shots[0].Source!.transform.rotation;
        var strongest = 0;
        for (var index = 0; index < shots.Count; index++)
        {
            var camera = shots[index].Source!; var weight = weights[index] / sum;
            position += camera.transform.position * weight;
            var delta = Mathf.Repeat(camera.transform.rotation - referenceAngle + 180, 360) - 180;
            rotation += (referenceAngle + delta) * weight;
            size += camera.size * weight;
            color = new(color.r + camera.backgroundColor.r * weight, color.g + camera.backgroundColor.g * weight,
                color.b + camera.backgroundColor.b * weight, color.a + camera.backgroundColor.a * weight);
            if (weights[index] >= weights[strongest]) strongest = index;
        }
        var dominant = shots[strongest].Source!;
        output.transform.position = position; output.transform.rotation = rotation;
        output.size = size; output.backgroundColor = color;
        output.clearMode = dominant.clearMode; output.cullingMask = dominant.cullingMask;
        output.viewportRect = dominant.viewportRect;
    }

    private sealed record CameraState(Vector2 Position, Fix64 Rotation, Fix64 Size, Color Background,
        CameraClearMode ClearMode, ulong CullingMask, Rect Viewport)
    {
        internal static CameraState Capture(Camera2D camera) => new(camera.transform.localPosition,
            camera.transform.localRotation, camera.size, camera.backgroundColor, camera.clearMode,
            camera.cullingMask, camera.viewportRect);
        internal void Restore(Camera2D camera)
        {
            camera.transform.localPosition = Position; camera.transform.localRotation = Rotation;
            camera.size = Size; camera.backgroundColor = Background; camera.clearMode = ClearMode;
            camera.cullingMask = CullingMask; camera.viewportRect = Viewport;
        }
    }
}
