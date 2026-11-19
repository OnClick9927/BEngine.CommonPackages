namespace BEngine;

public enum SpriteSkinningMode { Cpu, Gpu, Auto }

public readonly record struct SpriteSkinBoneTransform(Vector2 Origin, Vector2 XAxis, Vector2 YAxis)
{
    public Vector2 TransformPoint(Vector2 point) => Origin + XAxis * point.x + YAxis * point.y;
}

[AddComponentMenu("Rendering/Sprite Skin 2D")]
public sealed class SpriteSkin2D : Renderer2D
{
    public SpriteSkinAsset? skin { get; set; }
    public Transform? rootBone { get; set; }
    public Transform[] bones { get; set; } = [];
    public string attachment { get; set; } = "Default";
    public Color color { get; set; } = Color.white;
    public Vector2[] deformation { get; set; } = [];
    public SpriteSkinningMode skinningMode { get; set; } = SpriteSkinningMode.Auto;

    public SpriteSkinBoneTransform[] GetWorldBoneTransforms() => GetWorldBoneTransformsCore(true);
    internal SpriteSkinBoneTransform[] GetWorldBoneTransformsValidated() => GetWorldBoneTransformsCore(false);
    private SpriteSkinBoneTransform[] GetWorldBoneTransformsCore(bool validate)
    {
        var asset = skin ?? throw new InvalidOperationException("SpriteSkin2D has no skin asset.");
        if (validate) asset.Validate();
        if (bones.Length != asset.Bones.Count || Matches1265())
            if (!Rebind())
                throw new InvalidOperationException("SpriteSkin2D bone bindings are incomplete.");
        [global::BEngine.SkipLoopGuard]
        bool Matches1265()
        {
            var matchSource1265 = bones;
            ArgumentNullException.ThrowIfNull(matchSource1265, "source");
            global::System.ReadOnlySpan<global::BEngine.Transform> matchValues1265 = matchSource1265;
            foreach (var item in matchValues1265)
            {
                if (item is null)
                    return true;
            }
        
            return false;
        }
        var result = new SpriteSkinBoneTransform[bones.Length];
        for (var index = 0; index < bones.Length; index++)
        {
            var bind = asset.Bones[index]; var bone = bones[index];
            var origin = Map(Vector2.zero);
            result[index] = new(origin, Map(new(1, 0)) - origin, Map(new(0, 1)) - origin);
            Vector2 Map(Vector2 position)
            {
                var relative = Vector2.Rotate(position - bind.BindPosition, -bind.BindRotation);
                return bone.TransformPoint(new(relative.x / bind.BindScale.x, relative.y / bind.BindScale.y));
            }
        }
        return result;
    }

    public IReadOnlyList<string> attachments => skin?.Attachments.Select(item => item.Name).ToArray() ?? [];

    public bool SetAttachment(string name)
    {
        if (skin?.FindAttachment(name) is null) return false;
        attachment = name.Trim();
        return true;
    }

    public void SetDeformation(ReadOnlySpan<Vector2> offsets)
    {
        if (skin is null) throw new InvalidOperationException("SpriteSkin2D has no skin asset.");
        if (offsets.Length != skin.Vertices.Count)
            throw new ArgumentException("Deformation offset count must match the skin vertex count.", nameof(offsets));
        deformation = offsets.ToArray();
    }

    public void ClearDeformation() => deformation = [];

    public bool Rebind()
    {
        if (skin is null || rootBone is null) { bones = []; return false; }
        var byName = BuildIndex3403(Descendants(rootBone).GroupBy(item => item.gameObject.name, StringComparer.Ordinal), StringComparer.Ordinal);
        [global::BEngine.SkipLoopGuard]
        global::System.Collections.Generic.Dictionary<string, global::BEngine.Transform> BuildIndex3403(global::System.Collections.Generic.IEnumerable<global::System.Linq.IGrouping<string, global::BEngine.Transform>> indexSource3403, global::System.Collections.Generic.IEqualityComparer<string>? indexComparer3403)
        {
            ArgumentNullException.ThrowIfNull(indexSource3403, "source");
            var knownCount = indexSource3403.TryGetNonEnumeratedCount(out var capacity);
            if (knownCount && capacity == 0)
                return new global::System.Collections.Generic.Dictionary<string, global::BEngine.Transform>(indexComparer3403);
            global::System.ReadOnlySpan<global::System.Linq.IGrouping<string, global::BEngine.Transform>> values;
            if (knownCount && indexSource3403 is global::System.Linq.IGrouping<string, global::BEngine.Transform>[] array)
                values = array;
            else if (knownCount && indexSource3403 is global::System.Collections.Generic.List<global::System.Linq.IGrouping<string, global::BEngine.Transform>> list)
                values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list);
            else
            {
                var result = new global::System.Collections.Generic.Dictionary<string, global::BEngine.Transform>(capacity, indexComparer3403);
                foreach (var item in indexSource3403)
                    result.Add(IndexKey3403(item), IndexValue3403(item));
                return result;
            }
        
            {
                var result = new global::System.Collections.Generic.Dictionary<string, global::BEngine.Transform>(values.Length, indexComparer3403);
                foreach (var item in values)
                    result.Add(IndexKey3403(item), IndexValue3403(item));
                return result;
            }
        
            string IndexKey3403(global::System.Linq.IGrouping<string, global::BEngine.Transform> group) => group.Key;
            global::BEngine.Transform IndexValue3403(global::System.Linq.IGrouping<string, global::BEngine.Transform> group) => group.First();
        }
        var resolved = new Transform[skin.Bones.Count];
        for (var index = 0; index < resolved.Length; index++)
            if (!byName.TryGetValue(skin.Bones[index].Name, out resolved[index]!))
            { bones = resolved; return false; }
        bones = resolved;
        return true;
    }

    public Vector2[] GetDeformedLocalVertices() => GetDeformedLocalVerticesCore(true);
    private Vector2[] GetDeformedLocalVerticesCore(bool validate)
    {
        var asset = skin ?? throw new InvalidOperationException("SpriteSkin2D has no skin asset.");
        if (validate) asset.Validate();
        if (bones.Length != asset.Bones.Count || Matches3807())
            if (!Rebind())
                throw new InvalidOperationException("SpriteSkin2D bone bindings are incomplete.");
        [global::BEngine.SkipLoopGuard]
        bool Matches3807()
        {
            var matchSource3807 = bones;
            ArgumentNullException.ThrowIfNull(matchSource3807, "source");
            global::System.ReadOnlySpan<global::BEngine.Transform> matchValues3807 = matchSource3807;
            foreach (var item in matchValues3807)
            {
                if (item is null)
                    return true;
            }
        
            return false;
        }
        var result = new Vector2[asset.Vertices.Count];
        for (var vertexIndex = 0; vertexIndex < result.Length; vertexIndex++)
        {
            var vertex = asset.Vertices[vertexIndex];
            var value = Vector2.zero;
            foreach (var influence in vertex.Weights)
            {
                var bind = asset.Bones[influence.BoneIndex];
                var deformedPosition = vertex.Position +
                    (vertexIndex < deformation.Length ? deformation[vertexIndex] : Vector2.zero);
                var relative = Vector2.Rotate(deformedPosition - bind.BindPosition, -bind.BindRotation);
                relative = new Vector2(relative.x / bind.BindScale.x, relative.y / bind.BindScale.y);
                var world = bones[influence.BoneIndex].TransformPoint(relative);
                value += transform.InverseTransformPoint(world) * influence.Weight;
            }
            result[vertexIndex] = value;
        }
        return result;
    }

    public Vector2[] GetDeformedWorldVertices() => WorldVertices(true);
    internal Vector2[] GetDeformedWorldVerticesValidated() => WorldVertices(false);
    private Vector2[] WorldVertices(bool validate)
    {
        var vertices = GetDeformedLocalVerticesCore(validate);
        for (var index = 0; index < vertices.Length; index++) vertices[index] = transform.TransformPoint(vertices[index]);
        return vertices;
    }

    public SpriteSkinAttachment ResolveAttachment()
    {
        var asset = skin ?? throw new InvalidOperationException("SpriteSkin2D has no skin asset.");
        return asset.FindAttachment(attachment) ?? asset.Attachments.FirstOrDefault() ??
               throw new InvalidOperationException("Sprite skin has no attachments.");
    }

    public override void OnValidate()
    {
        if (skin is null) return;
        skin.Validate();
        if (skin.FindAttachment(attachment) is null && skin.Attachments.Count > 0)
            attachment = skin.Attachments[0].Name;
    }

    private static IEnumerable<Transform> Descendants(Transform root)
    {
        var pending = new Stack<Transform>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            yield return current;
            for (var index = current.childCount - 1; index >= 0; index--)
                pending.Push(current.GetChild(index));
        }
    }
}
