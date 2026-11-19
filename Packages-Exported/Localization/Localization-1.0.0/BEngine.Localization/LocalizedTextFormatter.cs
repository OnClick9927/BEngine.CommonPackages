using System.Globalization;
using System.Collections;
using System.Text;

namespace BEngine.Localization;

/// <summary>Named values, standard .NET formats and explicit plural branches; parsing never evaluates code.</summary>
public static class LocalizedTextFormatter
{
    private static readonly object WorkspaceGate = new();
    private static readonly ObjectPool<FormattingWorkspace> Workspaces = new(static () => new(),
        onRelease: static workspace => workspace.Reset(), maxSize: 32);

    public static string Format(string text, IReadOnlyDictionary<string, object?>? arguments = null, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Expand(text, arguments, culture ?? CultureInfo.InvariantCulture, null, 0);
    }

    /// <summary>Validates every branch and returns its named arguments without evaluating values.</summary>
    public static IReadOnlyList<string> GetArgumentNames(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var names = new SortedSet<string>(StringComparer.Ordinal);
        Inspect(text, names, 0);
        var result = new string[names.Count];
        var index = 0;
        foreach (var name in names) result[index++] = name;
        return result;
    }

    private static void Inspect(string text, SortedSet<string> names, int depth)
    {
        if (depth > 16) throw new FormatException("Localized text nesting exceeds 16 levels.");
        using var budget = LoopExecutionGuard.AllowIterations(text.Length + 1);
        var workspace = GetWorkspace();
        try { InspectCore(text, names, depth, workspace); }
        finally { ReleaseWorkspace(workspace); }
    }

    private static void InspectCore(string text, SortedSet<string> names, int depth, FormattingWorkspace workspace)
    {
        for (var index = 0; index < text.Length; index++)
        {
            var ch = text[index];
            if ((ch == '{' || ch == '}') && index + 1 < text.Length && text[index + 1] == ch) { index++; continue; }
            if (ch == '}') throw new FormatException("Unexpected closing brace in localized text.");
            if (ch != '{') continue;
            var end = FindClose(text, index);
            var expression = text[(index + 1)..end]; index = end;
            var colon = expression.IndexOf(':');
            var key = (colon < 0 ? expression : expression[..colon]).Trim();
            if (key.Length == 0) throw new FormatException("Localized argument names must not be empty.");
            names.Add(key);
            var format = colon < 0 ? "" : expression[(colon + 1)..];
            var prefixLength = format.StartsWith("plural:", StringComparison.Ordinal) ? 7
                : format.StartsWith("ordinal:", StringComparison.Ordinal) ? 8
                : format.StartsWith("select:", StringComparison.Ordinal) ? 7
                : format.StartsWith("if:", StringComparison.Ordinal) ? 3 : 0;
            if (prefixLength > 0)
                foreach (var branch in ReadBranches(format[prefixLength..], workspace.Branches).Values) Inspect(branch, names, depth + 1);
        }
    }

    private static string Expand(string text, IReadOnlyDictionary<string, object?>? args, CultureInfo culture, decimal? count, int depth)
    {
        if (depth > 16) throw new FormatException("Localized text nesting exceeds 16 levels.");
        using var budget = LoopExecutionGuard.AllowIterations(text.Length + 1);
        var workspace = GetWorkspace();
        try { return ExpandCore(text, args, culture, count, depth, workspace); }
        finally { ReleaseWorkspace(workspace); }
    }

    private static string ExpandCore(string text, IReadOnlyDictionary<string, object?>? args, CultureInfo culture,
        decimal? count, int depth, FormattingWorkspace workspace)
    {
        var output = workspace.Output;
        output.EnsureCapacity(text.Length);
        for (var index = 0; index < text.Length; index++)
        {
            var ch = text[index];
            if ((ch == '{' || ch == '}') && index + 1 < text.Length && text[index + 1] == ch)
            { output.Append(ch); index++; continue; }
            if (ch == '}') throw new FormatException("Unexpected closing brace in localized text.");
            if (ch != '{')
            {
                if (ch == '#' && count.HasValue) output.Append(count.Value.ToString(culture));
                else output.Append(ch);
                continue;
            }
            var end = FindClose(text, index);
            var expression = text[(index + 1)..end];
            index = end;
            var colon = expression.IndexOf(':');
            var key = (colon < 0 ? expression : expression[..colon]).Trim();
            if (args is null || !args.TryGetValue(key, out var value)) throw new FormatException($"Missing localized argument '{key}'.");
            var format = colon < 0 ? null : expression[(colon + 1)..];
            if (format?.StartsWith("plural:", StringComparison.Ordinal) == true || format?.StartsWith("ordinal:", StringComparison.Ordinal) == true)
            {
                decimal number;
                try { number = Convert.ToDecimal(value, CultureInfo.InvariantCulture); }
                catch (Exception error) when (error is FormatException or InvalidCastException or OverflowException)
                { throw new FormatException($"Plural argument '{key}' must be numeric.", error); }
                var ordinal = format.StartsWith("ordinal:", StringComparison.Ordinal);
                var branches = ReadBranches(format[(ordinal ? 8 : 7)..], workspace.Branches);
                var exact = "=" + number.ToString(CultureInfo.InvariantCulture);
                var pluralLocale = culture.Name.Length == 0 ? "en" : culture.Name;
                if (!branches.TryGetValue(exact, out var branch) && !branches.TryGetValue(CldrPluralRules.Select(number, pluralLocale, ordinal), out branch))
                    branch = branches["other"];
                output.Append(Expand(branch, args, culture, number, depth + 1));
            }
            else if (format?.StartsWith("select:", StringComparison.Ordinal) == true || format?.StartsWith("if:", StringComparison.Ordinal) == true)
            {
                var condition = format.StartsWith("if:", StringComparison.Ordinal);
                var branches = ReadBranches(format[(condition ? 3 : 7)..], workspace.Branches);
                var selector = condition ? (Truthy(value) ? "true" : "false") : Convert.ToString(value, culture) ?? "";
                output.Append(Expand(branches.GetValueOrDefault(selector, branches["other"]), args, culture, count, depth + 1));
            }
            else if (format?.StartsWith("list:", StringComparison.Ordinal) == true)
            {
                if (value is not IEnumerable list || value is string) throw new FormatException($"List argument '{key}' must be enumerable.");
                var separator = format[5..]; var items = workspace.Items;
                items.Clear();
                foreach (var item in list)
                {
                    if (items.Count >= 10000) throw new FormatException("Localized list exceeds 10000 items.");
                    items.Add(item is IFormattable formatted ? formatted.ToString(null, culture) : item?.ToString() ?? "");
                }
                output.Append(string.Join(separator, items));
            }
            else output.Append(value is IFormattable formattable ? formattable.ToString(format, culture) : value?.ToString());
        }
        return output.ToString();
    }

    private static int FindClose(string text, int start)
    {
        var nesting = 1;
        for (var index = start + 1; index < text.Length; index++)
        {
            if (text[index] == '{') nesting++;
            else if (text[index] == '}' && --nesting == 0) return index;
        }
        throw new FormatException("Unclosed brace in localized text.");
    }

    private static Dictionary<string, string> ReadBranches(string text, Dictionary<string, string> result)
    {
        result.Clear();
        var index = 0;
        while (index < text.Length)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
            if (index == text.Length) break;
            var brace = text.IndexOf('{', index);
            if (brace < 0) throw new FormatException("Expected a plural branch body.");
            var key = text[index..brace].Trim();
            var end = FindClose(text, brace);
            if (key.Length == 0 || !result.TryAdd(key, text[(brace + 1)..end]))
                throw new FormatException("Empty or duplicate plural branch.");
            index = end + 1;
        }
        if (!result.ContainsKey("other")) throw new FormatException("Plural formats require an 'other' branch.");
        return result;
    }

    public static string PluralCategory(decimal number, CultureInfo culture) => CldrPluralRules.Select(number, culture);
    public static string OrdinalCategory(decimal number, CultureInfo culture) => CldrPluralRules.Select(number, culture, ordinal: true);
    private static bool Truthy(object? value) => value switch
    { null => false, bool boolean => boolean, string text => text.Length > 0, IConvertible number => Convert.ToDecimal(number, CultureInfo.InvariantCulture) != 0, _ => true };

    public static string PseudoLocalize(string text)
    {
        using var budget = LoopExecutionGuard.AllowIterations(text.Length + 1);
        const string source = "aeiouAEIOU";
        const string replacement = "áëïöüÁËÏÖÜ";
        var workspace = GetWorkspace();
        try
        {
            var result = workspace.Output;
            result.Append('［');
            foreach (var ch in text)
            {
                var index = source.IndexOf(ch);
                result.Append(index < 0 ? ch : replacement[index]);
            }
            return result.Append('~', Math.Max(2, text.Length / 3)).Append('］').ToString();
        }
        finally { ReleaseWorkspace(workspace); }
    }

    private static FormattingWorkspace GetWorkspace()
    {
        lock (WorkspaceGate) return Workspaces.Get();
    }

    private static void ReleaseWorkspace(FormattingWorkspace workspace)
    {
        lock (WorkspaceGate) Workspaces.Release(workspace);
    }

    // Only borrowing/returning is locked. Recursive branches and user formatters own separate
    // workspaces, and no application callback runs while the pool gate is held.
    private sealed class FormattingWorkspace
    {
        private StringBuilder? _output;
        private Dictionary<string, string>? _branches;
        private List<string>? _items;
        internal StringBuilder Output => _output ??= new();
        internal Dictionary<string, string> Branches => _branches ??= new(StringComparer.Ordinal);
        internal List<string> Items => _items ??= [];
        internal void Reset()
        {
            if (_output is { Capacity: > 8192 }) _output = null;
            else _output?.Clear();
            if (_branches is not null)
            {
                _branches.Clear();
                if (_branches.EnsureCapacity(0) > 256) _branches = null;
            }
            if (_items is not null)
            {
                _items.Clear();
                if (_items.Capacity > 1024) _items = null;
            }
        }
    }
}
