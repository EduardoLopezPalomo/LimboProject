/*
 * AbilityNamePropertyDrawer.cs
 *
 * Draws [AbilityName] string fields as a dropdown of AbilityIds.All instead of a
 * free-text box (T-008 step 4). A mistyped ability name compiles, loads, and at
 * runtime simply never resolves - id = Animator.StringToHash(Name) - so the
 * Inspector must not let the typo be written in the first place.
 *
 * Design rules:
 *  - The stored value stays a plain string; no data migration, no API change.
 *  - A value that is NOT in the registry is appended to the popup as a visible
 *    "<value>  (NOT IN REGISTRY)" entry and the field is tinted, instead of being
 *    snapped to a valid name. Broken data stays obvious and fixable.
 *  - Selecting "(not set)" writes the empty string, matching the old free-text
 *    behavior.
 */
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AbilityNameAttribute))]
public sealed class AbilityNamePropertyDrawer : PropertyDrawer
{
    public const string NotSetLabel = "(not set)";
    public const string NotInRegistrySuffix = "  (NOT IN REGISTRY)";

    // Popup entries for a given current value; index answers which entry is the
    // current one. Public so verification can assert the mapping without a GUI frame.
    public static string[] BuildOptions(string current, out int index)
    {
        var options = new List<string>(AbilityIds.All.Count + 2) { NotSetLabel };
        options.AddRange(AbilityIds.All);

        if (string.IsNullOrEmpty(current))
        {
            index = 0;
        }
        else if (AbilityIds.IsKnownName(current))
        {
            index = options.IndexOf(current);
        }
        else
        {
            index = options.Count;
            options.Add(current + NotInRegistrySuffix);
        }
        return options.ToArray();
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            if (!_warnedNonString)
            {
                _warnedNonString = true;
                Debug.LogWarning($"[AbilityName] on non-string field '{property.propertyPath}' draws as a plain field.");
            }
            return;
        }

        bool mixed = property.hasMultipleDifferentValues;
        string current = mixed ? string.Empty : property.stringValue;
        string[] options;
        int index;
        if (mixed)
        {
            options = BuildOptions(string.Empty, out _);
            index = -1;
            EditorGUI.showMixedValue = true;
        }
        else
        {
            options = BuildOptions(current, out index);
        }

        bool unknown = !mixed && !string.IsNullOrEmpty(current) && !AbilityIds.IsKnownName(current);
        var labelled = new GUIContent(label)
        {
            text = unknown ? "⚠ " + label.text : label.text,
            tooltip = "Ability runtime Name from the AbilityIds registry. Replaces free text so a typo cannot be authored."
        };
        var contents = new GUIContent[options.Length];
        for (int i = 0; i < options.Length; i++) contents[i] = new GUIContent(options[i]);

        EditorGUI.BeginProperty(position, label, property);
        if (unknown)
            EditorGUI.DrawRect(position, new Color(1f, 0.4f, 0f, 0.25f)); // background, under the popup
        EditorGUI.BeginChangeCheck();
        int picked = EditorGUI.Popup(position, labelled, index, contents);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck() && picked >= 0)
        {
            // Mixed targets differ by definition, so any pick changes them;
            // single target writes only when the pick differs from the value.
            bool changed;
            string value;
            if (mixed)
            {
                value = ResolveOption(options, picked);
                changed = true;
            }
            else
            {
                changed = TryResolvePick(options, picked, current, out value);
            }
            if (changed) property.stringValue = value; // multi-target aware: writes all selected objects
        }
        EditorGUI.EndProperty();
    }

    bool _warnedNonString;

    // One mapping option -> stored value, shared by both branches: index 0 is
    // "(not set)" = empty; a flagged entry stores its raw value, suffix removed.
    static string ResolveOption(string[] options, int picked)
    {
        if (picked == 0) return string.Empty;
        string value = options[picked];
        int mark = value.IndexOf(NotInRegistrySuffix, System.StringComparison.Ordinal);
        return mark >= 0 ? value.Substring(0, mark) : value;
    }

    // What a popup selection means on a single target: true + `value` when the pick
    // changes the field, false for no-op picks (reselecting the current entry, index
    // out of range). Public so verification can assert the decision without a GUI frame.
    public static bool TryResolvePick(string[] options, int picked, string current, out string value)
    {
        value = picked >= 0 && picked < options.Length ? ResolveOption(options, picked) : current;
        return value != current;
    }
}
