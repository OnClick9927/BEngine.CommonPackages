using System.Collections;
using System.Globalization;
using System.Reflection;
using BEngine.Editor;
using BEngine.Platform;

namespace BEngine.Editor;

[CustomPropertyDrawer(typeof(ExtendedPropertyAttribute), useForChildren: true)]
public sealed class ExtendedPropertyDrawer : PropertyDrawer
{
    private MemberInfo? _attributeMember;
    private IReadOnlyList<Attribute>? _attributeSource;
    private ExtendedPropertyAttribute[] _orderedAttributes = [];
    private int[] _attributeOrders = [];
    public override Fix64 GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var attributes = Attributes();
        if (!IsVisible(property, attributes)) return 0;
        var target = property.serializedObject.targetObject;
        var spacing = EditorGUIUtility.standardVerticalSpacing;
        var height = EditorGUIUtility.singleLineHeight + spacing;
        height += (Fix64)attributes.OfType<HorizontalLineAttribute>()
            .Sum(item => item.thickness + 4);
        bool __queryResult1034;
        {
            var __querySource1034 = attributes;
            ArgumentNullException.ThrowIfNull(__querySource1034, "source");
            __queryResult1034 = false;
            foreach (var __queryItem1034 in __querySource1034)
            {
                if ((__queryItem1034 is TitleAttribute))
                {
                    __queryResult1034 = true;
                    break;
                }
            }
        }
        
        if (__queryResult1034)
            height += EditorGUIUtility.singleLineHeight + spacing;
        var width = EstimatedHelpBoxWidth();
        {
            var __filteredSource1205 = attributes.OfType<InfoBoxAttribute>();
            ArgumentNullException.ThrowIfNull(__filteredSource1205, "source");
            foreach (var info in __filteredSource1205)
            {
                if (!(info.visibleIf is null || EvaluateMember(property, info.visibleIf, null, false)))
                    continue;
                height += HelpBoxHeight(info.message, ToMessageType(info.messageType), width) + spacing;
            }
        }
        height += attributes.OfType<InlineButtonAttribute>().Count() *
                  (EditorGUIUtility.singleLineHeight + spacing);
        {
            var __filteredSource1631 = attributes.OfType<RequiredAttribute>();
            ArgumentNullException.ThrowIfNull(__filteredSource1631, "source");
            foreach (var required in __filteredSource1631)
            {
                if (!(IsMissing(property.boxedValue)))
                    continue;
                height += HelpBoxHeight(required.message, MessageType.Error, width) + spacing;
            }
        }
        {
            var __filteredSource1840 = attributes.OfType<ValidateInputAttribute>();
            ArgumentNullException.ThrowIfNull(__filteredSource1840, "source");
            foreach (var validation in __filteredSource1840)
            {
                if (!(!InvokeValidator(target, validation.validatorMember, property.boxedValue)))
                    continue;
                height += HelpBoxHeight(validation.message, ToMessageType(validation.messageType), width) + spacing;
            }
        }
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ArgumentNullException.ThrowIfNull(property);
        var attributes = Attributes();
        if (!IsVisible(property, attributes)) return;
        var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        {
            var __typedSource3195 = attributes;
            ArgumentNullException.ThrowIfNull(__typedSource3195, "source");
            foreach (var __typedItem3195 in __typedSource3195)
            {
                if (__typedItem3195 is not HorizontalLineAttribute rule)
                    continue;
                var color = new Color(Fix64.FromRaw(1417339207L) /* 0.33 */, Fix64.FromRaw(1503238553L) /* 0.35 */, Fix64.FromRaw(1589137899L) /* 0.37 */, 1);
                if (rule.color.Length == 7 && rule.color[0] == '#' && uint.TryParse(rule.color.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
                    color = new Color((Fix64)((rgb >> 16) & 255) / 255, (Fix64)((rgb >> 8) & 255) / 255, (Fix64)(rgb & 255) / 255, 1);
                GUI.DrawRect(new Rect(line.x, line.y + 2, line.width, (Fix64)rule.thickness), color);
                line = new Rect(line.x, line.y + (Fix64)rule.thickness + 4, line.width, line.height);
            }
        }
        if (attributes.OfType<TitleAttribute>().LastOrDefault() is { } title)
        {
            EditorGUI.LabelField(line, new GUIContent(string.IsNullOrWhiteSpace(title.subtitle) ? title.title :
                $"{title.title} - {title.subtitle}"), EditorStyles.boldLabel);
            line = Next(line);
        }
        {
            var __typedSource4314 = attributes;
            ArgumentNullException.ThrowIfNull(__typedSource4314, "source");
            foreach (var __typedItem4314 in __typedSource4314)
            {
                if (__typedItem4314 is not InfoBoxAttribute info)
                    continue;
                if (info.visibleIf is null || EvaluateMember(property, info.visibleIf, null, false))
                {
                    var type = ToMessageType(info.messageType);
                    line = DrawHelpBox(line, info.message, type);
                }
            }
        }
        var actualLabel = attributes.OfType<LabelTextAttribute>().LastOrDefault()?.label ?? label.text;
        var oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel += attributes.OfType<IndentAttribute>().LastOrDefault()?.level ?? 0;
        var oldEnabled = GUI.enabled;
        GUI.enabled &= IsEnabled(property, attributes) && Gather5924(attributes);
        [global::BEngine.SkipLoopGuard]
        bool Gather5924(global::System.Collections.Generic.IEnumerable<global::BEngine.ExtendedPropertyAttribute> items5924)
        {
            ArgumentNullException.ThrowIfNull(items5924, "source");
            bool Evaluate5924(global::BEngine.ExtendedPropertyAttribute item) => item is not ReadOnlyAttribute;
            global::System.ReadOnlySpan<global::BEngine.ExtendedPropertyAttribute> values;
            if (items5924.GetType() == typeof(global::BEngine.ExtendedPropertyAttribute[]))
                values = (global::BEngine.ExtendedPropertyAttribute[])items5924;
            else if (items5924.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.ExtendedPropertyAttribute>))
                values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.ExtendedPropertyAttribute>)items5924);
            else
                return global::System.Linq.Enumerable.All(items5924, Evaluate5924);
            foreach (var item in values)
                if (!Evaluate5924(item))
                    return false;
            return true;
        }
        try
        {
            var field = line;
            var fieldLabel = new GUIContent(actualLabel, tooltip: property.tooltip);
            var prefix = attributes.OfType<PrefixAttribute>().LastOrDefault();
            var suffix = attributes.OfType<SuffixAttribute>().LastOrDefault();
            if (prefix is not null || suffix is not null)
            {
                field = EditorGUI.PrefixLabel(field, fieldLabel);
                fieldLabel = GUIContent.none;
                if (prefix is not null)
                {
                    var width = Fix64.Min(field.width / 3, EditorStyles.label.CalcSize(new GUIContent(prefix.text)).x + 4);
                    GUI.Label(new Rect(field.x, field.y, width, field.height), prefix.text);
                    field = new Rect(field.x + width, field.y, field.width - width, field.height);
                }
                if (suffix is not null)
                {
                    var width = Fix64.Min(field.width / 3, EditorStyles.label.CalcSize(new GUIContent(suffix.text)).x + 4);
                    GUI.Label(new Rect(field.xMax - width, field.y, width, field.height), suffix.text);
                    field = new Rect(field.x, field.y, field.width - width, field.height);
                }
            }
            DrawField(field, property, fieldLabel, attributes);
        }
        finally
        {
            GUI.enabled = oldEnabled;
            EditorGUI.indentLevel = oldIndent;
        }
        ApplyConstraints(property, attributes);
        line = Next(line);
        {
            var __typedSource6645 = attributes;
            ArgumentNullException.ThrowIfNull(__typedSource6645, "source");
            foreach (var __typedItem6645 in __typedSource6645)
            {
                if (__typedItem6645 is not InlineButtonAttribute button)
                    continue;
                if (GUI.Button(EditorGUI.IndentedRect(line), button.label))
                {
                    property.serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(property.serializedObject.targetObject, button.label);
                    InvokeAction(property.serializedObject.targetObject, button.methodName, property.boxedValue);
                    property.serializedObject.Update();
                }
        
                line = Next(line);
            }
        }
        {
            var __filteredSource6552 = attributes.OfType<RequiredAttribute>();
            ArgumentNullException.ThrowIfNull(__filteredSource6552, "source");
            foreach (var required in __filteredSource6552)
            {
                if (!(IsMissing(property.boxedValue)))
                    continue;
                line = DrawHelpBox(line, required.message, MessageType.Error);
            }
        }
        {
            var __filteredSource6767 = attributes.OfType<ValidateInputAttribute>();
            ArgumentNullException.ThrowIfNull(__filteredSource6767, "source");
            foreach (var validation in __filteredSource6767)
            {
                if (!(!InvokeValidator(property.serializedObject.targetObject, validation.validatorMember, property.boxedValue)))
                    continue;
                line = DrawHelpBox(line, validation.message, ToMessageType(validation.messageType));
            }
        }
    }

    private void DrawField(Rect position, SerializedProperty property, GUIContent label,
        IReadOnlyList<ExtendedPropertyAttribute> attributes)
    {
        if (Gather9537(attributes) && property.boxedValue is Enum flags)
        {
            var field = EditorGUI.PrefixLabel(position, label);
            if (EditorGUI.DropdownButton(field, flags.ToString()))
            {
                var menu = new GenericMenu();
                var enumType = flags.GetType();
                var value = EnumBits(flags);
                foreach (Enum option in Enum.GetValues(enumType))
                {
                    var bits = EnumBits(option);
                    var selected = bits == 0 ? value == 0 : (value & bits) == bits;
                    menu.AddItem(new GUIContent(option.ToString()), selected, () =>
                    {
                        property.boxedValue = Enum.ToObject(enumType, bits == 0 ? 0UL : selected ? value & ~bits : value | bits);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }

                menu.DropDown(field);
            }

            return;
        }

        [global::BEngine.SkipLoopGuard]
        bool Gather9537(global::System.Collections.Generic.IEnumerable<global::BEngine.ExtendedPropertyAttribute> items9537)
        {
            ArgumentNullException.ThrowIfNull(items9537, "source");
            bool Evaluate9537(global::BEngine.ExtendedPropertyAttribute item) => item is EnumFlagsAttribute;
            global::System.ReadOnlySpan<global::BEngine.ExtendedPropertyAttribute> values;
            if (items9537.GetType() == typeof(global::BEngine.ExtendedPropertyAttribute[]))
                values = (global::BEngine.ExtendedPropertyAttribute[])items9537;
            else if (items9537.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.ExtendedPropertyAttribute>))
                values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.ExtendedPropertyAttribute>)items9537);
            else
                return global::System.Linq.Enumerable.Any(items9537, Evaluate9537);
            foreach (var item in values)
                if (Evaluate9537(item))
                    return true;
            return false;
        }
        if (attributes.OfType<DropdownAttribute>().LastOrDefault() is { } dropdown)
        {
            var choices = dropdown.providerMember is { Length: > 0 }
                ? ResolveChoices(property.serializedObject.targetObject, dropdown.providerMember).ToArray()
                : dropdown.choices.ToArray();
            var index = property.propertyType == SerializedPropertyType.Enum ? property.enumValueIndex :
                Math.Max(0, Array.IndexOf(choices, property.stringValue));
            index = EditorGUI.Popup(position, label.text, index, choices);
            if (property.propertyType == SerializedPropertyType.Enum) property.enumValueIndex = index;
            else if (choices.Length > 0) property.stringValue = choices[index];
            return;
        }
        if (attributes.OfType<ProgressBarAttribute>().LastOrDefault() is { } progress)
        {
            position = EditorGUI.IndentedRect(position);
            var value = property.floatValue;
            GUI.DrawRect(position, new Color(Fix64.FromRaw(515396075L) /* 0.12 */, Fix64.FromRaw(515396075L) /* 0.12 */,
                Fix64.FromRaw(515396075L) /* 0.12 */, 1));
            var t = (value - progress.minimum) / Math.Max(0.0001f, progress.maximum - progress.minimum);
            GUI.DrawRect(new Rect(position.x, position.y, position.width * (Fix64)Math.Clamp(t, 0, 1), position.height),
                new Color(Fix64.FromRaw(773094113L) /* 0.18 */, Fix64.FromRaw(2233382993L) /* 0.52 */, Fix64.FromRaw(3092376453L) /* 0.72 */, 1));
            GUI.Label(position, progress.title ?? $"{label.text}: {value:0.##}");
            if (progress.editable) property.floatValue = (float)GUI.HorizontalSlider(position, (Fix64)value,
                (Fix64)progress.minimum, (Fix64)progress.maximum);
            return;
        }
        if (attributes.OfType<MinMaxAttribute>().LastOrDefault() is { } minMax &&
            property.propertyType == SerializedPropertyType.Vector2)
        {
            var value = property.vector2Value;
            var updated = EditorGUI.Vector2Field(position, label.text, value);
            var minimum = (Fix64)Math.Min(minMax.minimum, minMax.maximum);
            var maximum = (Fix64)Math.Max(minMax.minimum, minMax.maximum);
            var low = Fix64.Clamp(updated.x, minimum, maximum);
            property.vector2Value = new Vector2(low, Fix64.Clamp(updated.y, low, maximum));
            return;
        }
        if (attributes.OfType<PasswordAttribute>().LastOrDefault() is { } password &&
            property.propertyType == SerializedPropertyType.String)
        {
            property.stringValue = GUI.PasswordField(EditorGUI.PrefixLabel(position, label), property.stringValue,
                password.mask); return;
        }
        if (Gather13457(attributes) && property.propertyType == SerializedPropertyType.String)
        {
            var field = EditorGUI.PrefixLabel(position, label);
            property.stringValue = GUI.TextField(new Rect(field.x, field.y, Fix64.Max(0, field.width - 28), field.height), property.stringValue);
            if (EditorToolbar.Button(new Rect(field.xMax - 26, field.y, 26, field.height), new GUIContent(string.Empty, EditorBuiltinIcons.Toolbar.Browse, "Browse")))
                SelectPath(property, attributes);
            return;
        }

        [global::BEngine.SkipLoopGuard]
        bool Gather13457(global::System.Collections.Generic.IEnumerable<global::BEngine.ExtendedPropertyAttribute> items13457)
        {
            ArgumentNullException.ThrowIfNull(items13457, "source");
            bool Evaluate13457(global::BEngine.ExtendedPropertyAttribute item) => item is FilePathAttribute or FolderPathAttribute or AssetPathAttribute;
            global::System.ReadOnlySpan<global::BEngine.ExtendedPropertyAttribute> values;
            if (items13457.GetType() == typeof(global::BEngine.ExtendedPropertyAttribute[]))
                values = (global::BEngine.ExtendedPropertyAttribute[])items13457;
            else if (items13457.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.ExtendedPropertyAttribute>))
                values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.ExtendedPropertyAttribute>)items13457);
            else
                return global::System.Linq.Enumerable.Any(items13457, Evaluate13457);
            foreach (var item in values)
                if (Evaluate13457(item))
                    return true;
            return false;
        }
        if (EditorReflectionCache.GetAttributes(memberInfo).OfType<RangeAttribute>().FirstOrDefault() is { } range &&
            property.propertyType is SerializedPropertyType.Integer or SerializedPropertyType.Float)
        {
            if (property.propertyType == SerializedPropertyType.Integer)
                property.intValue = EditorGUI.IntSlider(position, label.text, property.intValue,
                    (int)MathF.Ceiling(range.min), (int)MathF.Floor(range.max));
            else property.floatValue = EditorGUI.Slider(position, label.text, property.floatValue,
                range.min, range.max);
            return;
        }
        EditorGUI.DefaultPropertyField(position, property, label, true);
    }

    private ExtendedPropertyAttribute[] Attributes()
    {
        var source = EditorReflectionCache.GetAttributes(memberInfo);
        if (ReferenceEquals(_attributeMember, memberInfo) && ReferenceEquals(_attributeSource, source))
        {
            var unchanged = true;
            for (var index = 0; unchanged && index < _orderedAttributes.Length; index++)
                unchanged = _orderedAttributes[index].order == _attributeOrders[index];
            if (unchanged) return _orderedAttributes;
        }
        // The drawer follows its SerializedObject lifetime. Keep metadata here rather
        // than retaining reloaded members in a process-wide package dictionary.
        _orderedAttributes = source.OfType<ExtendedPropertyAttribute>().OrderBy(item => item.order).ToArray();
        _attributeOrders = Gather15715(_orderedAttributes);
        [global::BEngine.SkipLoopGuard]
        int[] Gather15715(global::System.Collections.Generic.IEnumerable<global::BEngine.ExtendedPropertyAttribute> items15715)
        {
            ArgumentNullException.ThrowIfNull(items15715, "source");
            int Evaluate15715(global::BEngine.ExtendedPropertyAttribute item) => item.order;
            global::System.ReadOnlySpan<global::BEngine.ExtendedPropertyAttribute> values;
            if (items15715.GetType() == typeof(global::BEngine.ExtendedPropertyAttribute[]))
                values = (global::BEngine.ExtendedPropertyAttribute[])items15715;
            else if (items15715.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.ExtendedPropertyAttribute>))
                values = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.ExtendedPropertyAttribute>)items15715);
            else
            {
                return global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Select(items15715, Evaluate15715));
            }

            if (values.IsEmpty)
                return global::System.Array.Empty<int>();
            var result = new int[values.Length];
            for (var index = 0; index < values.Length; index++)
                result[index] = Evaluate15715(values[index]);
            return result;
        }
        _attributeMember = memberInfo;
        _attributeSource = source;
        return _orderedAttributes;
    }
    private static ulong EnumBits(Enum value) => Type.GetTypeCode(Enum.GetUnderlyingType(value.GetType())) is
        TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64
            ? Convert.ToUInt64(value) : unchecked((ulong)Convert.ToInt64(value));
    private static bool IsVisible(SerializedProperty property, IEnumerable<ExtendedPropertyAttribute> attributes) =>
        attributes.OfType<ShowIfAttribute>().All(item => EvaluateCondition(property, item)) &&
        attributes.OfType<HideIfAttribute>().All(item => !EvaluateCondition(property, item));
    private static bool IsEnabled(SerializedProperty property, IEnumerable<ExtendedPropertyAttribute> attributes) =>
        attributes.OfType<EnableIfAttribute>().All(item => EvaluateCondition(property, item)) &&
        attributes.OfType<DisableIfAttribute>().All(item => !EvaluateCondition(property, item));
    private static bool EvaluateCondition(SerializedProperty property, ConditionalPropertyAttribute condition) =>
        EvaluateMember(property, condition.conditionMember, condition.expectedValue, condition.hasExpectedValue);
    private static bool EvaluateMember(SerializedProperty property, string name, object? expected, bool compare)
    {
        object? value;
        var condition = property.serializedObject.FindProperty(name);
        if (condition is not null)
        {
            if (!condition.TryGetBoxedValue(out value)) return false;
        }
        else if (!TryGetMemberValue(property.serializedObject.targetObject, name, out value)) return false;
        if (!compare) return value switch { bool boolean => boolean, null => false, string text => text.Length > 0, _ => true };
        if (value is null) return expected is null;
        try
        {
            var converted = expected is null ? null : value.GetType().IsEnum
                ? expected is string text ? Enum.Parse(value.GetType(), text, true) : Enum.ToObject(value.GetType(), expected)
                : Convert.ChangeType(expected, value.GetType(), CultureInfo.InvariantCulture);
            return Equals(value, converted);
        }
        catch { return Equals(value, expected); }
    }
    private static bool TryGetMemberValue(object target, string name, out object? value)
    {
        return EditorReflectionCache.TryGetMemberValue(target, name, out value);
    }
    private static void ApplyConstraints(SerializedProperty property, ExtendedPropertyAttribute[] attributes)
    {
        if (property.propertyType is not (SerializedPropertyType.Integer or SerializedPropertyType.Float)) return;
        var minimum = double.NegativeInfinity;
        var maximum = double.PositiveInfinity;
        var hasMinimum = false;
        var hasMaximum = false;
        foreach (var attribute in attributes)
        {
            if (attribute is ClampAttribute clamp)
            {
                // Enumerable.Max ignores NaN after a numeric value; Min preserves
                // NaN. Retain those rules and first-value ties without iterators.
                if (!hasMinimum || double.IsNaN(minimum) || clamp.minimum > minimum) minimum = clamp.minimum;
                if (!hasMaximum || double.IsNaN(clamp.maximum) || clamp.maximum < maximum) maximum = clamp.maximum;
                hasMinimum = hasMaximum = true;
            }
        }
        foreach (var attribute in attributes)
            if (attribute is MaxValueAttribute max)
            {
                if (!hasMaximum || double.IsNaN(max.maximum) || max.maximum < maximum) maximum = max.maximum;
                hasMaximum = true;
            }
        var constrained = Math.Clamp(property.doubleValue, minimum, maximum);
        if (Math.Abs(constrained - property.doubleValue) > double.Epsilon) property.doubleValue = constrained;
    }
    private static IReadOnlyList<string> ResolveChoices(object target, string providerMember) =>
        TryGetMemberValue(target, providerMember, out var value) && value is IEnumerable values
            ? values.Cast<object?>().Select(item => item?.ToString() ?? string.Empty).ToArray() : [];
    private static bool InvokeValidator(object target, string name, object? value)
    {
        return EditorReflectionCache.TryInvokePredicate(target, name, value, out var result) && result;
    }
    private static void InvokeAction(object target, string name, object? value)
    {
        EditorReflectionCache.TryInvokeAction(target, name, value);
    }
    private static bool IsMissing(object? value) => value switch
    { null => true, string text => string.IsNullOrWhiteSpace(text), BObject engineObject => engineObject == null, _ => false };
    private static MessageType ToMessageType(ValidationMessageType type) => type switch
    { ValidationMessageType.Warning => MessageType.Warning, ValidationMessageType.Error => MessageType.Error, _ => MessageType.Info };
    private static Rect Next(Rect line) => new(line.x, line.y + EditorGUIUtility.singleLineHeight + 2,
        line.width, line.height);
    private static Rect DrawHelpBox(Rect line, string message, MessageType type)
    {
        var height = HelpBoxHeight(message, type, line.width);
        EditorGUI.HelpBox(new Rect(line.x, line.y, line.width, height), message, type);
        return new Rect(line.x, line.y + height + EditorGUIUtility.standardVerticalSpacing,
            line.width, EditorGUIUtility.singleLineHeight);
    }
    private static Fix64 HelpBoxHeight(string message, MessageType type, Fix64 width) =>
        EditorGUI.GetHelpBoxHeight(message, type, width);
    private static Fix64 EstimatedHelpBoxWidth() =>
        Fix64.Max(1, EditorGUIUtility.currentViewWidth - 36);
    private static void SelectPath(SerializedProperty property, IReadOnlyList<ExtendedPropertyAttribute> attributes)
    {
        var paths = EditorServices.GetRequiredService<PathUtility>();
        var platform = EditorServices.GetRequiredService<PlatformUtility>();
        var projectRoot = paths.GetDirectoryName(paths.GetFullPath(Application.dataPath)) ?? platform.CurrentDirectory;
        void Accept(string path, bool relative) => property.stringValue = relative
            ? paths.GetRelativePath(projectRoot, path).Replace('\\', '/') : paths.GetFullPath(path);
        if (attributes.OfType<FilePathAttribute>().LastOrDefault() is { } file)
            EditorFileDialog.Open(file.title, projectRoot, file.filter, path => Accept(path, file.relativeToProject));
        else if (attributes.OfType<FolderPathAttribute>().LastOrDefault() is { } folder)
            EditorFileDialog.OpenFolder(folder.description, projectRoot, path => Accept(path, folder.relativeToProject));
        else if (attributes.OfType<AssetPathAttribute>().LastOrDefault() is { } asset)
        {
            var filter = string.IsNullOrWhiteSpace(asset.extension) ? "All assets (*.*)|*.*" :
                $"Assets (*.{asset.extension!.TrimStart('.')})|*.{asset.extension.TrimStart('.')}";
            EditorFileDialog.Open("Select Asset", Application.dataPath, filter, path => Accept(path, true));
        }
    }
}
