using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;
using Object = UnityEngine.Object;

/// <summary>
/// P1.1 guard rail: keeps AbilityIds (the code side registry) and the ability
/// references that live in DATA in sync, in BOTH directions.
///
/// What counts as an ability reference (only these three shapes):
///   1. AttackDefinition.ability                       (ScriptableObject string field)
///   2. behavior graph "Ability" fields                (Unity.Behavior BlackboardVariable&lt;string&gt;)
///      used by ActivateAbilityAction / CancelAbilityAction / WaitForAbilityEndAction
///   3. UnityEvent string arguments (m_Mode: 5) of persistent calls to
///      AbilityHolder.ActivateAbility / CancelAbility (a.k.a. Player.prefab button bindings)
/// Plus the two registry checks:
///   4. every GameplayAbility asset Name exists in AbilityIds. Two assets may share a Name as
///      long as they live in different AbilityHolders, so that case is only a warning.
///   5. every AbilityIds const is still referenced, by an asset or by code (AbilityIds.<Name>)
///
/// Deliberate NON-checks: Animator parameter names ("Dash", "CounterAttack", ...),
/// attribute names ("BlockResistance"), blackboard variable NAMES, node/class names and
/// any other string that merely looks like an ability. Those are not ability references.
///
/// The validator only logs (Debug.LogError per problem + one summary line). It never
/// throws, never modifies an asset, and never blocks an import.
/// Runs from Tools/Ciro/Validate Ability References, and automatically once per Editor session
/// (a cold pass costs ~15s, a warm one ~1.5s, so it does not repeat after every script reload).
/// </summary>
public static class AbilityReferenceValidator
{
    const string Tag = "[AbilityReferenceValidator] ";

    /// <summary>Prefix of the serialized class line of a record inside a behavior graph asset.</summary>
    const string TypePrefix = "      type: {class: ";

    /// Third-party copies and leftovers: never reported.
    /// Assets/DownloadPackages/StarterAssets/.../PlayerArmature.prefab is a copy of
    /// Player.prefab that is not the shipped player, so its bindings are not our business.
    static readonly string[] ExcludedPrefixes =
    {
        "Assets/DownloadPackages/",
        "Assets/Synty/",
        "Assets/_Recovery/",
    };

    class Issue
    {
        public string Message;
        public Object Context;
    }

    class Counts
    {
        public int abilityAssets;
        public int attackDefs;
        public int graphs;
        public int graphAbilityFields;
        public int graphFieldsSkipped;
        public int eventFiles;
        public int eventBindings;
        public int eventNonStringBindings;
    }

    [MenuItem("Tools/Ciro/Validate Ability References")]
    public static void ValidateMenuItem()
    {
        Validate();
    }

    /// <summary>
    /// Session flag: the first pass of an Editor session costs ~15s (cold asset loading), a warm
    /// one ~1.5s, so the automatic run happens once per session instead of after every reload.
    /// </summary>
    const string RanThisSessionKey = "Ciro.AbilityReferenceValidator.ranThisSession";

    [InitializeOnLoadMethod]
    static void ValidateAfterReload()
    {
        // A full pass reads 12 behavior graphs, every prefab/scene and every .cs. Once per Editor
        // session is enough for a background guard; the menu item always gives a fresh pass.
        if (SessionState.GetBool(RanThisSessionKey, false)) return;
        EditorApplication.delayCall += AfterReloadDelay;
    }

    static int reloadRetries;

    static void AfterReloadDelay()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            if (reloadRetries++ < 40) EditorApplication.delayCall += AfterReloadDelay;
            return;
        }
        reloadRetries = 0;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(RanThisSessionKey, true);
        Validate();
    }

    /// <summary>Runs every check and logs the result. Returns the number of problems + warnings found.</summary>
    public static int Validate()
    {
        var sw = Stopwatch.StartNew();
        var issues = new List<Issue>();
        var warnings = new List<Issue>();
        var used = new HashSet<string>(StringComparer.Ordinal);
        var counts = new Counts();

        try
        {
            CollectCodeUsage(used);
            CheckAbilityAssets(issues, warnings, used, counts);
            CheckAttackDefinitions(issues, used, counts);
            CheckBehaviorGraphs(issues, used, counts);
            CheckEventBindings(issues, used, counts);
            CheckRegistryIsUsed(issues, used);
        }
        catch (Exception e)
        {
            // Never let a validator problem break the editor or an import.
            Debug.LogWarning(Tag + "validation did not finish (" + e.GetType().Name + ": " + e.Message
                + "). Re-run Tools/Ciro/Validate Ability References once the cause is fixed.");
        }

        sw.Stop();
        foreach (var issue in issues) Debug.LogError(Tag + issue.Message, issue.Context);
        foreach (var warning in warnings) Debug.LogWarning(Tag + warning.Message, warning.Context);

        string summary = Tag + "checked " + counts.abilityAssets + " ability assets, "
            + AbilityIds.All.Count + " registry names, " + counts.attackDefs + " attack definitions, "
            + counts.graphs + " behavior graphs (" + counts.graphAbilityFields + " Ability fields, "
            + counts.graphFieldsSkipped + " non-string skipped), " + counts.eventFiles
            + " prefab/scene files (" + counts.eventBindings + " ActivateAbility/CancelAbility bindings, "
            + counts.eventNonStringBindings + " non-string bindings ignored) in "
            + sw.ElapsedMilliseconds + "ms";
        if (issues.Count == 0 && warnings.Count == 0) Debug.Log(summary + " -> OK, no ability-reference drift.");
        else Debug.LogWarning(summary + " -> " + issues.Count + " problem(s) and " + warnings.Count + " warning(s) logged above.");
        return issues.Count + warnings.Count;
    }

    // ------------------------------------------------------------------ 4 + 1
    // GameplayAbility assets: Name must be registered. Two assets sharing one Name is legal
    // while they belong to different AbilityHolders (the boss' "ElectroAttackBoss" asset and the
    // player's "ElectroAttack" asset are different characters), so that is only a warning.
    // ------------------------------------------------------------------
    static void CheckAbilityAssets(List<Issue> issues, List<Issue> warnings, HashSet<string> used, Counts counts)
    {
        var byName = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var path in FindAssets("t:GameplayAbility"))
        {
            var ability = AssetDatabase.LoadAssetAtPath<GameplayAbility>(path);
            if (ability == null)
            {
                Add(issues, "could not load GameplayAbility at " + path, null);
                continue;
            }
            counts.abilityAssets++;

            if (!AbilityIds.IsKnownName(ability.Name))
            {
                Add(issues, path + " has Name=" + Q(ability.Name)
                    + " which is not registered in AbilityIds. Either the asset was renamed (remember: Name is hashed into the ability Id and into saved data, so renames need a migration) or this is a new ability that needs a const in AbilityIds.",
                    ability);
                continue;
            }
            used.Add(ability.Name);

            List<string> paths;
            if (!byName.TryGetValue(ability.Name, out paths))
            {
                paths = new List<string>(2);
                byName[ability.Name] = paths;
            }
            paths.Add(path);
        }

        foreach (var pair in byName)
        {
            if (pair.Value.Count < 2) continue;
            Add(warnings, pair.Value.Count + " GameplayAbility assets share Name=" + Q(pair.Key) + ": "
                + string.Join(", ", pair.Value)
                + ". They resolve to the same ability Id, which is fine as long as each one sits in a different AbilityHolder - check that no single holder holds two of them.",
                null);
        }
    }

    // ------------------------------------------------------------------ 1
    // AttackDefinition.ability
    // ------------------------------------------------------------------
    static void CheckAttackDefinitions(List<Issue> issues, HashSet<string> used, Counts counts)
    {
        foreach (var path in FindAssets("t:AttackDefinition"))
        {
            var def = AssetDatabase.LoadAssetAtPath<AttackDefinition>(path);
            if (def == null)
            {
                Add(issues, "could not load AttackDefinition at " + path, null);
                continue;
            }
            counts.attackDefs++;

            var prop = new SerializedObject(def).FindProperty("ability");
            if (prop == null)
            {
                Add(issues, path + " has no serialized 'ability' field - the validator cannot read it.", def);
                continue;
            }

            if (AbilityIds.IsKnownName(prop.stringValue))
            {
                used.Add(prop.stringValue);
                continue;
            }
            Add(issues, path + ":" + FindFieldLine(path, "ability", prop.stringValue) + " ability=" + Q(prop.stringValue)
                + " is not registered in AbilityIds.", def);
        }
    }

    // ------------------------------------------------------------------ 2
    // Unity.Behavior graphs: "Ability" fields of the ability action nodes.
    // The .asset is a serialized object graph, so the value is resolved through the
    // field's LocalValue (or LinkedVariable) blackboard record.
    // ------------------------------------------------------------------
    class GraphRecord
    {
        public long Rid;
        // Serialized class of the record: an action node ("ActivateAbilityAction"),
        // a "BehaviorGraphNodeModel/FieldModel", or a "BlackboardVariable`1[[System.String, mscorlib]]".
        public string ClassName = "";
        public string Name = "";
        public string Value = "";
        public string FieldName = "";
        public long LocalRid;
        public long LinkedRid;
        public int ValueLine;
        // rids this record points at. For a node these are its fields ("Ability: rid: ..."),
        // which is how a field record is traced back to the node that shows it.
        public List<long> ChildRids;
    }

    static void CheckBehaviorGraphs(List<Issue> issues, HashSet<string> used, Counts counts)
    {
        foreach (var path in FindAssets("t:BehaviorAuthoringGraph"))
        {
            counts.graphs++;

            Dictionary<long, GraphRecord> records;
            try
            {
                records = ParseGraph(path);
            }
            catch (Exception e)
            {
                Add(issues, path + " could not be parsed (" + e.Message + "), its Ability fields were NOT checked.", null);
                continue;
            }
            if (records.Count == 0)
            {
                Add(issues, path + " parsed to 0 records, its Ability fields were NOT checked.", null);
                continue;
            }

            // which node owns which field record, so a message can name the node ("ActivateAbilityAction")
            var ownerOfField = new Dictionary<long, GraphRecord>(records.Count);
            foreach (var record in records.Values)
            {
                if (record.ChildRids == null) continue;
                for (int i = 0; i < record.ChildRids.Count; i++) ownerOfField[record.ChildRids[i]] = record;
            }

            foreach (var field in records.Values)
            {
                if (field.ClassName != "BehaviorGraphNodeModel/FieldModel") continue;
                if (field.FieldName != "Ability") continue;
                counts.graphAbilityFields++;

                GraphRecord owner;
                string node = ownerOfField.TryGetValue(field.Rid, out owner) ? owner.ClassName : "unknown node";

                GraphRecord valueRecord = null;
                string via = "local value";
                if (field.LocalRid > 0 && records.TryGetValue(field.LocalRid, out valueRecord)) { }
                else if (field.LinkedRid > 0 && records.TryGetValue(field.LinkedRid, out valueRecord)) via = "linked variable " + Q(valueRecord.Name);
                else valueRecord = null;

                if (valueRecord == null)
                {
                    Add(issues, path + " " + node + ": its Ability field has neither a local nor a linked value, so it would resolve to an empty ability name at runtime.",
                        AssetDatabase.LoadAssetAtPath<Object>(path));
                    continue;
                }
                // Only string blackboard variables hold ability names; anything else (e.g. an
                // object reference named Ability) is not something this check can judge.
                if (valueRecord.ClassName.IndexOf("System.String", StringComparison.Ordinal) < 0)
                {
                    counts.graphFieldsSkipped++;
                    continue;
                }

                if (AbilityIds.IsKnownName(valueRecord.Value))
                {
                    used.Add(valueRecord.Value);
                    continue;
                }
                Add(issues, path + ":" + valueRecord.ValueLine + " " + node + " Ability=" + Q(valueRecord.Value)
                    + " (" + via + ") is not registered in AbilityIds.",
                    AssetDatabase.LoadAssetAtPath<Object>(path));
            }
        }
    }

    static Dictionary<long, GraphRecord> ParseGraph(string assetPath)
    {
        var records = new Dictionary<long, GraphRecord>(8192);
        GraphRecord current = null;
        string lastKey = "";
        int lineNo = 0;

        foreach (var line in File.ReadLines(assetPath))
        {
            lineNo++;
            if (line.Length < 9) continue;

            if (line.StartsWith("    - rid: ", StringComparison.Ordinal))
            {
                long rid;
                if (long.TryParse(After(line, "    - rid:"), out rid) && rid > 0)
                {
                    current = new GraphRecord { Rid = rid };
                    records[rid] = current;
                }
                else current = null;
                lastKey = "";
                continue;
            }
            if (current == null) continue;

            if (line.StartsWith(TypePrefix, StringComparison.Ordinal))
            {
                current.ClassName = ClassOf(line.Substring(TypePrefix.Length));
                continue;
            }
            // A "        key:" line opens a value: inline on this line, or (for a reference)
            // as a "          rid: N" line right below it.
            if (line[8] != ' ' && line[8] != '-')
            {
                var keyEnd = line.IndexOf(':', 8, line.Length - 8);
                if (keyEnd < 0) continue;
                var key = line.Substring(8, keyEnd - 8);
                var inline = line.Substring(keyEnd + 1).Trim();
                lastKey = key;
                if (inline.Length > 0)
                {
                    if (key == "FieldName") current.FieldName = Unquote(inline);
                    else if (key == "Name") current.Name = Unquote(inline);
                    else if (key == "m_Value")
                    {
                        current.Value = Unquote(inline);
                        current.ValueLine = lineNo;
                    }
                }
                continue;
            }

            if (line.StartsWith("          rid: ", StringComparison.Ordinal) ||
                line.StartsWith("        - rid: ", StringComparison.Ordinal))
            {
                long rid;
                if (!long.TryParse(After(line, "rid:"), out rid) || rid <= 0) continue;
                if (lastKey == "LocalValue") current.LocalRid = rid;
                else if (lastKey == "LinkedVariable") current.LinkedRid = rid;
                else if (lastKey != "Graph" && lastKey != "m_Parent")
                {
                    // a node pointing at one of its own fields (Ability, Duration, ...)
                    if (current.ChildRids == null) current.ChildRids = new List<long>(8);
                    current.ChildRids.Add(rid);
                }
            }
        }
        return records;
    }

    // ------------------------------------------------------------------ 3
    // Persistent UnityEvent bindings (Player.prefab buttons, scene objects, ...).
    // Text scan: these live inside prefab/scene YAML, not inside a component we can read.
    // ------------------------------------------------------------------
    static void CheckEventBindings(List<Issue> issues, HashSet<string> used, Counts counts)
    {
        var dataPath = Application.dataPath.Replace('\\', '/');
        var files = new List<string>();
        files.AddRange(Directory.GetFiles(Application.dataPath, "*.prefab", SearchOption.AllDirectories));
        files.AddRange(Directory.GetFiles(Application.dataPath, "*.unity", SearchOption.AllDirectories));

        foreach (var file in files)
        {
            var path = "Assets" + file.Replace('\\', '/').Substring(dataPath.Length);
            if (IsExcluded(path)) continue;
            counts.eventFiles++;
            ScanEventFile(file, path, issues, used, counts);
        }
    }

    static void ScanEventFile(string file, string path, List<Issue> issues, HashSet<string> used, Counts counts)
    {
        using (var reader = new StreamReader(file))
        {
            string line;
            int lineNo = 0;
            var target = "";

            while ((line = reader.ReadLine()) != null)
            {
                lineNo++;

                if (line.IndexOf("m_TargetAssemblyTypeName:", StringComparison.Ordinal) >= 0)
                {
                    target = After(line, "m_TargetAssemblyTypeName:");
                    continue;
                }
                if (line.IndexOf("m_MethodName:", StringComparison.Ordinal) < 0) continue;

                var method = After(line, "m_MethodName:");
                if (method != "ActivateAbility" && method != "CancelAbility") continue;

                int methodLine = lineNo;
                var mode = "";
                var argument = null as string;

                // The block continues for a handful of lines: m_Mode, m_Arguments{...}, m_CallState.
                for (int ahead = 0; ahead < 12; ahead++)
                {
                    var next = reader.ReadLine();
                    if (next == null) break;
                    lineNo++;
                    if (next.IndexOf("m_MethodName:", StringComparison.Ordinal) >= 0) break;
                    if (next.IndexOf("m_Mode:", StringComparison.Ordinal) >= 0) mode = After(next, "m_Mode:");
                    else if (next.IndexOf("m_StringArgument:", StringComparison.Ordinal) >= 0) argument = After(next, "m_StringArgument:");
                    if (mode.Length > 0 && argument != null) break;
                }

                counts.eventBindings++;
                // m_Mode 5 = the string overload. Anything else (e.g. ActivateEquippedAbility(int))
                // carries a leftover m_StringArgument that Unity ignores - not a reference.
                if (mode != "5")
                {
                    counts.eventNonStringBindings++;
                    continue;
                }
                if (AbilityIds.IsKnownName(argument))
                {
                    used.Add(argument);
                    continue;
                }
                Add(issues, path + ":" + methodLine + " a persistent UnityEvent calls " + method + "(string) with " + Q(argument)
                    + " on " + target + ", which is not registered in AbilityIds.",
                    AssetDatabase.LoadAssetAtPath<Object>(path));
            }
        }
    }

    // ------------------------------------------------------------------ 5
    // Every registered name must still be referenced, by data or by code.
    // ------------------------------------------------------------------
    static void CheckRegistryIsUsed(List<Issue> issues, HashSet<string> used)
    {
        for (int i = 0; i < AbilityIds.All.Count; i++)
        {
            var name = AbilityIds.All[i];
            if (used.Contains(name)) continue;
            Add(issues, "AbilityIds registers " + Q(name) + " but no GameplayAbility asset, AttackDefinition, behavior graph, event binding and no \"AbilityIds." + name
                + "\" code reference uses it. If the ability was deleted, remove the const too; if an asset was renamed, restore the name (renaming an asset also changes its saved Id).",
                null);
        }
    }

    /// <summary>
    /// A name used from C# as "AbilityIds.&lt;Name&gt;" counts as used: a name that only code needs
    /// (a cancel triggered from script, an Animator sync, ...) is not drift, and reporting it would
    /// push people towards deleting a const that is doing real work.
    /// </summary>
    static void CollectCodeUsage(HashSet<string> used)
    {
        const string marker = "AbilityIds.";
        var dataPath = Application.dataPath.Replace('\\', '/');

        foreach (var file in Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories))
        {
            var path = "Assets" + file.Replace('\\', '/').Substring(dataPath.Length);
            if (IsExcluded(path)) continue;

            string text;
            try
            {
                text = File.ReadAllText(file);
            }
            catch (Exception)
            {
                continue;
            }
            if (text.IndexOf(marker, StringComparison.Ordinal) < 0) continue;

            var at = 0;
            while ((at = text.IndexOf(marker, at, StringComparison.Ordinal)) >= 0)
            {
                at += marker.Length;
                var end = at;
                while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_')) end++;
                if (end == at) continue;
                var name = text.Substring(at, end - at);
                if (AbilityIds.IsKnownName(name)) used.Add(name);
            }
        }
    }

    // ------------------------------------------------------------------ helpers
    static void Add(List<Issue> issues, string message, Object context)
    {
        issues.Add(new Issue { Message = message, Context = context });
    }

    static string Q(string value)
    {
        return value == null ? "<null>" : "\"" + value + "\"";
    }

    static string After(string line, string key)
    {
        var value = line.Substring(line.IndexOf(key, StringComparison.Ordinal) + key.Length).Trim();
        if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"') value = value.Substring(1, value.Length - 2);
        return value;
    }

    /// <summary>Strips the double quotes YAML puts around a string value ("HealTarget" -> HealTarget).</summary>
    static string Unquote(string value)
    {
        if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"') return value.Substring(1, value.Length - 2);
        return value;
    }

    /// <summary>
    /// Reads the class out of "type: {class: ...}" without touching the namespace part:
    /// generic classes are single quoted ("class: 'BlackboardVariable`1[[System.String, mscorlib]]'")
    /// so the comma inside them must not be treated as a separator.
    /// </summary>
    static string ClassOf(string raw)
    {
        var value = raw.Trim();
        if (value.Length > 1 && value[0] == '\'')
        {
            var end = value.IndexOf('\'', 1);
            if (end > 1) return value.Substring(1, end - 1);
        }
        var comma = value.IndexOf(',');
        return comma > 0 ? value.Substring(0, comma).Trim() : value;
    }

    static bool IsExcluded(string path)
    {
        for (int i = 0; i < ExcludedPrefixes.Length; i++)
        {
            if (path.StartsWith(ExcludedPrefixes[i], StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    static List<string> FindAssets(string filter)
    {
        var paths = new List<string>();
        var guids = AssetDatabase.FindAssets(filter);
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!IsExcluded(path)) paths.Add(path);
        }
        paths.Sort(StringComparer.OrdinalIgnoreCase);
        return paths;
    }

    /// <summary>Best effort line number of a serialized field inside a .asset file (0 if not found).</summary>
    static int FindFieldLine(string assetPath, string field, string value)
    {
        var prefix = field + ":";
        int lineNo = 0;
        try
        {
            foreach (var line in File.ReadLines(assetPath))
            {
                lineNo++;
                var trimmed = line.Trim();
                if (!trimmed.StartsWith(prefix, StringComparison.Ordinal)) continue;
                if (After(trimmed, prefix) == (value ?? "")) return lineNo;
            }
        }
        catch (Exception)
        {
            // line numbers are cosmetic; never fail a check over them
        }
        return 0;
    }
}
