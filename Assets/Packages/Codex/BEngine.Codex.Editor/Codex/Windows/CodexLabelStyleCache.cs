namespace BEngine.Editor.Codex;

// Window-owned and bounded; skin changes rebuild styles instead of retaining old themes.
internal sealed class CodexLabelStyleCache
{
    private readonly Dictionary<(Fix64 Size, Color Color), GUIStyle> _styles = [];
    private GUIStyle? _source;
    internal void Clear() { _styles.Clear(); _source = null; }
    internal GUIStyle Get(Fix64 size, Color color)
    {
        var source = EditorStyles.label;
        if (!ReferenceEquals(_source, source)) { _styles.Clear(); _source = source; }
        if (_styles.TryGetValue((size, color), out var cached)) return cached;
        var style = new GUIStyle(source) { fontSize = size, alignment = TextAnchor.UpperLeft };
        style.normal.textColor = style.hover.textColor = style.focused.textColor = style.active.textColor = color;
        if (_styles.Count < 64) _styles.Add((size, color), style);
        return style;
    }
}

