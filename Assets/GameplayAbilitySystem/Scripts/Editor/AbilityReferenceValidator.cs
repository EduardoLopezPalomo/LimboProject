using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
/// And three convention checks, riding on the same walk of every first-party .cs:
///   6. the player's movement lock is only touched through InputController (T-001). Anyone calling
///      the StarterAssets setter directly is rebuilding the bug where an interrupted dash or throw
///      leaves the player permanently unable to move.
///   7. new code must not reach for a manager through a static singleton (P1.3, T-023). The 147
///      accesses the game already depends on are frozen per file, so only a new coupling is warned
///      about and the existing debt is not re-reported on every reload.
///   8. a layer is only resolved by name through Layers (T-014), and every name Layers is asked for
///      still exists in Project Settings. Unity answers a name it does not know with -1 / an empty mask
///      instead of an error, so a layer renamed in the Project Settings window silently turns into
///      "matches nothing": enemies stop seeing the player and area attacks find no targets.
/// And one check that reads the scenes themselves instead of the code:
///   9. every PlayerUIController in a scene that is in the build has all five of its menu slots filled (T-056).
///      Assets/Characters/Player.prefab ships those slots empty and each scene fills them in on its own Player
///      instance, so Unity never reports a gap either: an empty slot was a NullReferenceException on the pause
///      key, on the back key, or inside the death animation event where a throw is invisible.
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
        public int singletonFiles;
        public int singletonSites;
        public int singletonNew;
        public int layerResolverSites;
        public int layerLookups;
        public int layerNames;
        public int menuSlotScenes;
        public int menuSlotInstances;
        public int menuSlotGaps;
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
            CollectCodeUsage(used, issues, warnings, counts);
            CheckAbilityAssets(issues, warnings, used, counts);
            CheckAttackDefinitions(issues, used, counts);
            CheckBehaviorGraphs(issues, used, counts);
            CheckEventBindings(issues, used, counts);
            CheckRegistryIsUsed(issues, used);
            CheckLayerNamesExist(issues, counts);
            CheckMenuSlots(issues, counts);
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
            + counts.eventNonStringBindings + " non-string bindings ignored), "
            + counts.singletonSites + " raw singleton accesses in " + counts.singletonFiles
            + " files (" + counts.singletonNew + " beyond the frozen baseline), "
            + counts.layerResolverSites + " layer lookups routed through Layers, "
            + counts.layerNames + " resolver names checked against Project Settings ("
            + counts.layerLookups + " raw lookup outside the resolver), "
            + counts.menuSlotInstances + " Player menu-slot sets in " + counts.menuSlotScenes
            + " built scenes (" + counts.menuSlotGaps + " empty) in "
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
    /// Walks every first-party .cs once. Four things come out of it: which AbilityIds names the code
    /// uses, and the three convention checks (6, 7 and 8) that ride along instead of opening the files again.
    ///
    /// The sources are kept in memory for the pass because check 7 needs to know which types declare a
    /// singleton, and a type is only declared in one file while it is used in twenty. 305 small files
    /// cost a couple of MB for the length of a run that already reads every prefab in the project.
    ///
    /// A name used from C# as "AbilityIds.&lt;Name&gt;" counts as used: a name that only code needs
    /// (a cancel triggered from script, an Animator sync, ...) is not drift, and reporting it would
    /// push people towards deleting a const that is doing real work.
    /// </summary>
    static void CollectCodeUsage(HashSet<string> used, List<Issue> issues, List<Issue> warnings, Counts counts)
    {
        const string marker = "AbilityIds.";

        var sources = ReadFirstPartySources();
        var singletons = CollectSingletonTypes(sources);

        for (int s = 0; s < sources.Count; s++)
        {
            var path = sources[s].Key;
            var text = sources[s].Value;

            // Checks 6, 7 and 8 ride along in this walk instead of opening every file a second time.
            CheckInputLockConvention(path, text, issues);
            CheckSingletonConvention(path, text, singletons, warnings, counts);
            CheckLayerConvention(path, text, issues, counts);

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

    // ------------------------------------------------------------------ 6
    // The player's movement lock (T-001). The StarterAssets bool behind ThirdPersonController's
    // misspelled setter has no owner, no timer and no reset, so a caller that pairs its own false/true
    // by hand leaves the player permanently unable to move as soon as anything interrupts the pair -
    // death, knockback, a level change, a cancelled ability, a clip cut short before its restoring
    // event. That was 13 call sites in 7 files until the seam in Assets/Scripts/Player/InputController.cs
    // folded them all into a named, self-clearing lock. Nothing else may open that door again.
    //
    // Matching on the member access (".Set..."/".InputAvaible") keeps mentions inside doc comments out
    // of the report, and ".InputAvaible" catches a direct write to the public field. GetInputAviable()
    // is deliberately not matched: reading the flag is what InputController.Update does to self-heal.
    // ------------------------------------------------------------------
    static void CheckInputLockConvention(string path, string text, List<Issue> issues)
    {
        const string Seam = "Assets/Scripts/Player/InputController.cs";
        const string Setter = ".SetInputAviable";
        const string Field = ".InputAvaible";

        if (path == Seam) return;

        // The grep is exempt from the grep: this file has to spell the setter's name to look for it.
        if (path.EndsWith("AbilityReferenceValidator.cs", StringComparison.Ordinal)) return;

        for (var at = text.IndexOf(Setter, StringComparison.Ordinal); at >= 0; at = text.IndexOf(Setter, at + 1, StringComparison.Ordinal))
        {
            Add(issues, path + ":" + LineOf(text, at) + " freezes the player's movement through the StarterAssets "
                + "controller directly. Use InputController.AcquireInputLock(owner, holder) and ReleaseInputLock "
                + "(the ability's own AbilityIds name as holder): the seam is counted, it releases itself when the "
                + "character is disabled, and it survives an interrupted ability. This call does none of that (T-001).",
                null);
        }

        for (var at = text.IndexOf(Field, StringComparison.Ordinal); at >= 0; at = text.IndexOf(Field, at + 1, StringComparison.Ordinal))
        {
            Add(issues, path + ":" + LineOf(text, at) + " writes the StarterAssets movement field directly, which "
                + "bypasses the lock bookkeeping in InputController and can strand the player unable to move. "
                + "Use InputController.AcquireInputLock / ReleaseInputLock instead (T-001).",
                null);
        }
    }

    // ------------------------------------------------------------------ 7
    // The P1.3 rule: new code is handed the manager it needs instead of reaching for a static
    // singleton. A singleton is a global, so a class that uses one cannot be built under test, and it
    // reads null at exactly the moments people forget about - a scene that does not carry the manager,
    // an Awake that ran before the manager's, a level that unloads it first. The game already has
    // hundreds of these accesses (GameplayMenu, MenuNavigation, SaveSystem, the ability specs, the UI
    // menus, ...). Re-writing all of them is a project of its own, so the debt is FROZEN per file
    // rather than re-reported: SingletonDebt records what each file has today, and the check warns
    // only when a file goes past its own number. Deleting one is always welcome - after a cleanup run
    // Tools/Ciro/Print Singleton Debt and paste the smaller table back in.
    //
    // What counts as a singleton is decided by the source, not by a hand-kept name list: any type that
    // declares a static Instance/instance member (SingletonDeclaration). That is what makes the check
    // cheap to trust - BindingFlags.Instance, the seven reflection calls in GraphBindingValidator, is
    // not a singleton because nothing in this project declares it, so it is never reported and nobody
    // has to maintain a deny-list for it.
    //
    // Comments and string bodies are blanked first (BlankOutNonCode), because a mention of a manager in
    // documentation or in a log string is not a dependency, and a guard that reports documentation is
    // a guard that gets switched off.
    // ------------------------------------------------------------------

    /// <summary>A static member named Instance/instance: the declaration of a singleton.</summary>
    static readonly Regex SingletonDeclaration =
        new Regex("\\bstatic\\s+(?:readonly\\s+)?([A-Za-z_][A-Za-z0-9_]*)\\s+[iI]nstance\\b");

    /// <summary>An access through such a member: MusicManager.Instance, DayNight.instance, ...</summary>
    static readonly Regex SingletonAccess =
        new Regex("\\b([A-Za-z_][A-Za-z0-9_]*)\\.[iI]nstance\\b");

    /// <summary>
    /// The freeze, taken 11/16/2026 at 147 accesses in 57 files, and re-frozen smaller after each cleanup since
    /// (T-013 took it to 143). One entry per file that reaches for a manager today. A file listed here may keep
    /// that many accesses or fewer, never more; a file that is not listed has never been allowed any.
    /// Regenerate with Tools/Ciro/Print Singleton Debt.
    /// </summary>
    static readonly Dictionary<string, int> SingletonDebt =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { "Assets/GameplayAbilitySystem/Scripts/AttributeResponses/BossHealthResponse.cs", 1 },
            { "Assets/GameplayAbilitySystem/Scripts/AttributeResponses/EnemyHealthResponse.cs", 1 },
            { "Assets/GameplayAbilitySystem/Scripts/AttributeResponses/PlayerHealthResponse.cs", 2 },
            { "Assets/GameplayAbilitySystem/Scripts/NewAbilities/BowAttackSpec.cs", 1 },
            { "Assets/GameplayAbilitySystem/Scripts/NewAbilities/DamageProjectileSpec.cs", 2 },
            { "Assets/GameplayAbilitySystem/Scripts/NewAbilities/ElectroAttackSpec.cs", 1 },
            { "Assets/GameplayAbilitySystem/Scripts/NewAbilities/FireAttackSpec.cs", 1 },
            { "Assets/GameplayAbilitySystem/Scripts/NewAbilities/HealPlayerSpec.cs", 1 },
            { "Assets/GameplayAbilitySystem/Scripts/NewAbilities/MageCounterAttackSpec.cs", 1 },
            { "Assets/GameplayAbilitySystem/Scripts/NewAbilities/TeleportSpec.cs", 2 },
            { "Assets/NPC/Scripts/DeactivateNpcAction.cs", 1 },
            { "Assets/Scripts/Camera/RegisterListener.cs", 2 },
            { "Assets/Scripts/ChangeMainMenuPC.cs", 1 },
            { "Assets/Scripts/CharacterSFX.cs", 2 },
            { "Assets/Scripts/CharacterVFX.cs", 3 },
            { "Assets/Scripts/Combat/DamageResolver.cs", 6 },
            { "Assets/Scripts/Enemies/Boss/BossConversation.cs", 2 },
            { "Assets/Scripts/Enemies/EnemiesGroupController.cs", 2 },
            { "Assets/Scripts/Enemies/EnemyGraphBridge.cs", 1 },
            { "Assets/Scripts/Enemies/GoingToPlayer.cs", 3 },
            { "Assets/Scripts/Enemies/RotateAlongPlayer.cs", 1 },
            { "Assets/Scripts/Managers/PlayerSubSlot.cs", 2 },
            { "Assets/Scripts/NPC/GuardBlockerNPC.cs", 2 },
            { "Assets/Scripts/NPC/NPCGraphBridge.cs", 2 },
            { "Assets/Scripts/NPC/NPCPool.cs", 2 },
            { "Assets/Scripts/Player/LockOn.cs", 5 },
            { "Assets/Scripts/QuestItemInteractable.cs", 2 },
            { "Assets/Scripts/Quests/Goals/Goals.cs", 1 },
            { "Assets/Scripts/Quests/Quest.cs", 3 },
            { "Assets/Scripts/Quests/QuestZoneTrigger.cs", 1 },
            { "Assets/Scripts/SaveData/GameData.cs", 1 },
            { "Assets/Scripts/SaveData/GameLoader.cs", 7 },
            { "Assets/Scripts/SaveData/SaveSystem.cs", 4 },
            { "Assets/Scripts/UI/Credits/CreditsSceneController.cs", 2 },
            { "Assets/Scripts/UI/DeathMenu.cs", 2 },
            { "Assets/Scripts/UI/DialogueNPC.cs", 5 },
            { "Assets/Scripts/UI/DialogueUI.cs", 3 },
            { "Assets/Scripts/UI/DisplayMenu.cs", 6 },
            { "Assets/Scripts/UI/GameplayMenu.cs", 4 },
            { "Assets/Scripts/UI/InterfaceMenu.cs", 8 },
            { "Assets/Scripts/UI/LevelLoader.cs", 6 },
            { "Assets/Scripts/UI/MainMenu.cs", 4 },
            { "Assets/Scripts/UI/MenuNavigation.cs", 6 },
            { "Assets/Scripts/UI/Notifications/NotificationUI.cs", 1 },
            { "Assets/Scripts/UI/PauseMenu.cs", 5 },
            { "Assets/Scripts/UI/PlayerHUDMenu.cs", 4 },
            { "Assets/Scripts/UI/PlayerHealth.cs", 1 },
            { "Assets/Scripts/UI/PlayerUIController.cs", 1 },
            { "Assets/Scripts/UI/PopUpUI.cs", 2 },
            { "Assets/Scripts/UI/QuestListUI.cs", 1 },
            { "Assets/Scripts/UI/Sound/ButtonSound.cs", 2 },
            { "Assets/Scripts/UI/Sound/SliderSound.cs", 1 },
            { "Assets/Scripts/UI/Sound/ToggleSound.cs", 2 },
            { "Assets/Scripts/UI/Video/VideoSceneController.cs", 4 },
            { "Assets/Scripts/World/Teleport/TeleportQuest.cs", 2 },
            { "Assets/Scripts/World/Tutorial/TutorialTrigger.cs", 1 },
            { "Assets/VFX/Free Slash VFX/Demo/Scripts/ParticleAnimationEvent.cs", 1 },
        };

    static void CheckSingletonConvention(string path, string text, HashSet<string> singletons,
        List<Issue> warnings, Counts counts)
    {
        // The grep is exempt from the grep: this file has to spell both patterns to look for them.
        if (path.EndsWith("AbilityReferenceValidator.cs", StringComparison.Ordinal)) return;

        var code = BlankOutNonCode(text);
        var matches = SingletonAccess.Matches(code);
        if (matches.Count == 0) return;

        var allowed = 0;
        SingletonDebt.TryGetValue(path, out allowed);

        var seen = 0;
        var countedHere = false;
        for (var i = 0; i < matches.Count; i++)
        {
            // Not every ".Instance" is a singleton: BindingFlags.Instance is a .NET enum member, and a
            // type nobody here declares is not a singleton either way.
            if (!singletons.Contains(matches[i].Groups[1].Value)) continue;

            counts.singletonSites++;
            if (!countedHere) { countedHere = true; counts.singletonFiles++; }
            seen++;
            if (seen <= allowed) continue;

            counts.singletonNew++;

            Add(warnings, path + ":" + LineOf(code, matches[i].Index) + " reaches for " + matches[i].Value
                + (allowed == 0
                    ? ", and this file is not allowed any raw singleton access"
                    : ", which is access number " + seen + " in a file frozen at " + allowed)
                + ". Pass the manager in instead (a SerializeField, a parameter, or one lookup in the "
                + "scene bootstrap handed to whoever needs it): a static is null in exactly the scenes "
                + "nobody tested, and it cannot be replaced in a test. Going the other way is the fix - "
                + "run Tools/Ciro/Print Singleton Debt and paste the smaller table into "
                + "AbilityReferenceValidator.SingletonDebt (P1.3).",
                null);
        }
    }

    /// <summary>
    /// Replaces every comment and every string/char literal body with spaces, keeping the length and
    /// every line break, so a mention of a manager inside documentation or inside a log message is not
    /// mistaken for code, and LineOf() still reports the real line of whatever survives.
    /// Interpolated strings are blanked whole, which can hide a call written inside curly braces -
    /// a guard that only ever under-reports is the safe direction for a guard like this one.
    /// </summary>
    static string BlankOutNonCode(string text)
    {
        var buf = text.ToCharArray();
        var i = 0;
        while (i < buf.Length)
        {
            var c = buf[i];

            if (c == '/' && i + 1 < buf.Length && (buf[i + 1] == '/' || buf[i + 1] == '*'))
            {
                var block = buf[i + 1] == '*';
                buf[i] = ' ';
                buf[i + 1] = ' ';
                i += 2;
                while (i < buf.Length)
                {
                    if (!block && buf[i] == '\n') break;
                    if (block && buf[i] == '*' && i + 1 < buf.Length && buf[i + 1] == '/')
                    {
                        buf[i] = ' ';
                        buf[i + 1] = ' ';
                        i += 2;
                        break;
                    }
                    if (buf[i] != '\n' && buf[i] != '\r') buf[i] = ' ';
                    i++;
                }
                continue;
            }

            if (c == '@' && i + 1 < buf.Length && buf[i + 1] == '"')
            {
                buf[i] = ' ';
                i++;
                buf[i] = ' ';
                i++;
                while (i < buf.Length)
                {
                    if (buf[i] == '"')
                    {
                        if (i + 1 < buf.Length && buf[i + 1] == '"')
                        {
                            buf[i] = ' ';
                            buf[i + 1] = ' ';
                            i += 2;
                            continue;
                        }
                        buf[i] = ' ';
                        i++;
                        break;
                    }
                    if (buf[i] != '\n' && buf[i] != '\r') buf[i] = ' ';
                    i++;
                }
                continue;
            }

            if (c == '"' || c == '\'')
            {
                buf[i] = ' ';
                i++;
                while (i < buf.Length && buf[i] != c && buf[i] != '\n')
                {
                    if (buf[i] == '\\')
                    {
                        buf[i] = ' ';
                        i++;
                        if (i >= buf.Length || buf[i] == '\n') break;
                    }
                    buf[i] = ' ';
                    i++;
                }
                if (i < buf.Length && buf[i] == c)
                {
                    buf[i] = ' ';
                    i++;
                }
                continue;
            }

            i++;
        }
        return new string(buf);
    }

    /// <summary>
    /// Every first-party .cs as (asset path, text), honouring ExcludedPrefixes. The single source of
    /// the file list for every consumer: the AbilityIds scan, the three convention checks and
    /// Tools/Ciro/Print Singleton Debt.
    /// </summary>
    static List<KeyValuePair<string, string>> ReadFirstPartySources()
    {
        var dataPath = Application.dataPath.Replace('\\', '/');
        var sources = new List<KeyValuePair<string, string>>(320);

        foreach (var file in Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories))
        {
            var path = "Assets" + file.Replace('\\', '/').Substring(dataPath.Length);
            if (IsExcluded(path)) continue;
            try
            {
                sources.Add(new KeyValuePair<string, string>(path, File.ReadAllText(file)));
            }
            catch (Exception)
            {
                // A file we cannot read is not a finding; every other check simply does not see it.
            }
        }
        return sources;
    }

    /// <summary>The types that declare a static Instance/instance member somewhere in those sources.</summary>
    static HashSet<string> CollectSingletonTypes(List<KeyValuePair<string, string>> sources)
    {
        var singletons = new HashSet<string>(StringComparer.Ordinal);
        for (var s = 0; s < sources.Count; s++)
        {
            var declared = SingletonDeclaration.Matches(sources[s].Value);
            for (var i = 0; i < declared.Count; i++) singletons.Add(declared[i].Groups[1].Value);
        }
        return singletons;
    }

    /// <summary>
    /// Prints check 7's table in the exact shape of the SingletonDebt field, so a cleanup is recorded
    /// by pasting the smaller table over the old one. Read-only.
    /// </summary>
    [MenuItem("Tools/Ciro/Print Singleton Debt")]
    public static void PrintSingletonDebt()
    {
        var sources = ReadFirstPartySources();
        var singletons = CollectSingletonTypes(sources);
        var perFile = new Dictionary<string, int>(StringComparer.Ordinal);
        var total = 0;

        for (var s = 0; s < sources.Count; s++)
        {
            if (sources[s].Key.EndsWith("AbilityReferenceValidator.cs", StringComparison.Ordinal)) continue;
            var matches = SingletonAccess.Matches(BlankOutNonCode(sources[s].Value));
            for (var i = 0; i < matches.Count; i++)
            {
                if (!singletons.Contains(matches[i].Groups[1].Value)) continue;
                var file = sources[s].Key;
                var had = 0;
                perFile.TryGetValue(file, out had);
                perFile[file] = had + 1;
                total++;
            }
        }

        var lines = new List<string>(perFile.Count);
        foreach (var pair in perFile) lines.Add("        { \"" + pair.Key + "\", " + pair.Value + " },");
        lines.Sort(StringComparer.Ordinal);

        Debug.Log(Tag + "singleton debt is " + total + " accesses in " + perFile.Count
            + " files. Paste these between the braces of SingletonDebt to re-freeze:"
            + "\n" + string.Join("\n", lines));
    }

    // ------------------------------------------------------------------ 8
    // Layers, resolved by name (T-014). Unity has no opinion about a layer name it does not know:
    // LayerMask.NameToLayer answers -1 and LayerMask.GetMask answers 0, and both are perfectly usable
    // values as far as the physics API is concerned. So editing a layer name in Project Settings does not
    // break anything loudly - it changes the game. `layer == NameToLayer("Player")` can never be true,
    // an OverlapSphere filtered by a 0 mask finds nobody, and a weapon sweep silently hits nothing at all.
    // That is the same class of failure as the two checks above: a rule that only exists in prose, in a
    // project where the failure mode is "combat stops working" and the console stays clean.
    //
    // Assets/Scripts/Layers.cs is the seam: it resolves once per run, reports a name it cannot find by
    // name, and chooses the inert-but-visible answer. Two halves to keep that true - nobody may go around
    // the seam, and the names the seam advertises must still exist in Project Settings. The second half
    // is the one that actually catches a rename before someone has to play long enough to notice it.
    //
    // The pattern is matched against BlankOutNonCode output, where comments and string bodies are already
    // spaces, so documentation that mentions LayerMask.GetMask("EnemyLayer") - which Layers.cs itself is
    // full of - cannot be mistaken for a call, and no call hides inside a string either.
    // ------------------------------------------------------------------

    /// <summary>The seam: the one file allowed to ask Unity for a layer by name.</summary>
    const string LayerSeam = "Assets/Scripts/Layers.cs";

    /// <summary>A call that resolves a layer by name.</summary>
    static readonly Regex RawLayerLookup =
        new Regex("\\bLayerMask\\s*\\.\\s*(NameToLayer|GetMask)\\s*\\(");

    /// <summary>A call that goes through the resolver instead (the coverage number in the summary).</summary>
    static readonly Regex ResolverCall =
        new Regex("\\bLayers\\s*\\.\\s*(MaskOf|Is|IndexOf|TryIndex)\\s*\\(");

    static void CheckLayerConvention(string path, string text, List<Issue> issues, Counts counts)
    {
        // The seam is the seam, and the grep is exempt from the grep: this file spells both APIs to look for them.
        if (path == LayerSeam) return;
        if (path.EndsWith("AbilityReferenceValidator.cs", StringComparison.Ordinal)) return;

        var code = BlankOutNonCode(text);

        var through = ResolverCall.Matches(code);
        counts.layerResolverSites += through.Count;

        var matches = RawLayerLookup.Matches(code);
        for (var i = 0; i < matches.Count; i++)
        {
            counts.layerLookups++;
            Add(issues, path + ":" + LineOf(code, matches[i].Index) + " resolves a layer by name with "
                + matches[i].Value + " instead of going through Layers.MaskOf / Layers.Is / Layers.IndexOf. "
                + "A name that no longer exists is not an error to Unity: NameToLayer answers -1 and GetMask "
                + "answers 0, so whoever renamed that layer in Project Settings turned this call into "
                + "\"matches nothing\" - no damage, no targets, no console entry. The resolver reports the "
                + "name and returns the safe answer (T-014).", null);
        }
    }

    /// <summary>
    /// Every name Layers advertises in Layers.Known must be a layer Project Settings actually defines.
    /// This is the half that catches the rename: the convention check above proves the call went through
    /// the resolver, and this proves the resolver was asked for something real.
    /// </summary>
    static void CheckLayerNamesExist(List<Issue> issues, Counts counts)
    {
        // InternalEditorUtility.layers lists the defined layer names with the empty slots removed, so it
        // is a set to test membership against and NOT an index-aligned array (position 8 is EnemyLayer,
        // whose real index is 9, because slot 3 is unused).
        var available = UnityEditorInternal.InternalEditorUtility.layers;
        var defined = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < available.Length; i++) defined.Add(available[i]);

        var known = Layers.Known;
        if (known == null) return;
        counts.layerNames = known.Length;

        for (var i = 0; i < known.Length; i++)
        {
            if (defined.Contains(known[i])) continue;
            Add(issues, "Layers.Known asks for the layer " + Q(known[i]) + " and Project Settings > Tags and "
                + "Layers does not define it. It defines " + available.Length + ": "
                + string.Join(", ", available) + ". Everything filtering by the missing name now matches "
                + "nothing - enemies stop seeing the player, sweeps hit thin air - and nothing on the console "
                + "says why. Restore the layer name, or update Layers.cs to the new one (T-014).", null);
        }
    }

    // ------------------------------------------------------------------ 9
    // PlayerUIController's five menu slots (T-056). The component dereferenced them bare, so an empty slot was a
    // NullReferenceException on the first Back press - or inside PlayerDeath.anim's animation event, where a
    // throw is invisible and the player is left dead with no menu. The code half is fixed in the component (it
    // names every gap once as the scene loads); this half catches it before anyone has to play the scene.
    //
    // Why a scene scan and not a component scan: Assets/Characters/Player.prefab leaves all five slots EMPTY on
    // purpose. The panels belong to each scene's own UI canvas, so every scene fills the slots in on its own
    // Player instance. That makes the prefab never a finding and a scene that forgot one always a finding - and
    // it means the only honest answer to "is the death menu wired up in Forest?" is the prefab default overridden
    // by that scene's modifications, which is what Unity applies at load and what a grep of either file alone
    // cannot see. The print tool shows all three shapes: the prefab with five empties, the built scenes with
    // five fills each, and the one scene in the repo with real gaps.
    //
    // Scope, stated instead of assumed: only scenes enabled in Build Settings are reported as problems. A scene
    // outside the build cannot be played, and Assets/Scenes/SampleScene (a StarterAssets leftover, outside the
    // build, missing DeathMenu and OptionsMenu) would otherwise pin a permanent, unreachable problem onto the
    // baseline. Tools/Ciro/Print Menu Slots prints every scene, built or not, so the scope is never a hiding place.
    // ------------------------------------------------------------------

    /// <summary>The five GameObject slots PlayerUIController moves the player between.</summary>
    static readonly string[] MenuSlots = { "playerUI", "PauseMenu", "AblityMenu", "OptionsMenu", "DeathMenu" };

    /// <summary>One component block of a given script found in a prefab or scene file.</summary>
    class ComponentBlock
    {
        public long BlockID;
        public int Line;
        public readonly bool[] Assigned = new bool[MenuSlots.Length];
    }

    /// <summary>A PlayerUIController living inside a prefab, so the scenes override its slots one by one.</summary>
    class MenuSlotSource
    {
        public string PrefabPath;
        public string PrefabGuid;
        public ComponentBlock Block;
    }

    /// <summary>A PlayerUIController as the running scene will see it: defaults plus that scene's overrides.</summary>
    class MenuSlotInstance
    {
        public string OwnerPath;
        public string Label;
        public int Line;
        public readonly bool[] Assigned = new bool[MenuSlots.Length];
    }

    static void CheckMenuSlots(List<Issue> issues, Counts counts)
    {
        var scriptGuid = MenuScriptGuid(issues);
        if (scriptGuid == null) return;

        var sources = new List<MenuSlotSource>();
        CollectMenuSlotSources(scriptGuid, sources);
        if (sources.Count == 0)
        {
            Add(issues, "no prefab in the project carries a PlayerUIController (script " + Q(scriptGuid)
                + "), so check 9 has nothing to compare scene overrides against. If the component moved out of a "
                + "prefab into the scenes, this check has to be pointed at the scenes instead (T-056).", null);
            return;
        }

        var scenes = EditorBuildSettings.scenes;
        var instanceTotal = 0;
        for (var s = 0; s < scenes.Length; s++)
        {
            var path = scenes[s].path;
            if (!scenes[s].enabled || IsExcluded(path) || !File.Exists(path)) continue;

            var instances = ScanMenuSlotInstances(path, scriptGuid, sources);
            if (instances.Count == 0) continue;   // no Player placed in this scene (Main Menu, Video, Credits)
            counts.menuSlotScenes++;
            instanceTotal += instances.Count;

            for (var i = 0; i < instances.Count; i++)
            {
                var instance = instances[i];
                counts.menuSlotInstances++;
                for (var slot = 0; slot < MenuSlots.Length; slot++)
                {
                    if (instance.Assigned[slot]) continue;
                    counts.menuSlotGaps++;
                    Add(issues, instance.OwnerPath + ":" + instance.Line + " " + instance.Label + " has no "
                        + MenuSlots[slot] + ", so in this scene " + MenuSlotConsequence(MenuSlots[slot]) + ". "
                        + "Assets/Characters/Player.prefab ships all five menu slots empty and every scene fills "
                        + "them in on its own Player instance, so a copied scene, a deleted UI canvas or a wrong "
                        + "prefab dropped into a slot is invisible to Unity: PlayerUIController now warns once at "
                        + "Start instead of throwing, and Tools/Ciro/Print Menu Slots lists every scene (T-056).",
                        AssetDatabase.LoadAssetAtPath<Object>(instance.OwnerPath));
                }
            }
        }

        // The anti-sleep guard, and the reason this check prints its own counts in the summary. A parser that
        // stopped matching - because the YAML changed shape, or because the component moved into a nested prefab -
        // reports "0 empty slots", which reads exactly like a project with nothing wrong. The first version of
        // this check did precisely that (it parsed "fileID:" without the space YAML puts after the colon), and only
        // the count being 0 gave it away. Every playable scene in this game carries a Player, so seeing none at
        // all is a broken check, not a clean project, and is reported as one.
        if (instanceTotal == 0)
        {
            Add(issues, "check 9 found no Player instance in ANY scene that is enabled in Build Settings, so it "
                + "checked nothing. Either no built scene carries a Player (then this check is obsolete and should "
                + "go), or it can no longer see one and every real gap would pass silently. Run "
                + "Tools/Ciro/Print Menu Slots: it lists every scene in the project, and an empty list means the "
                + "scan, not the project, is the problem (T-056).", null);
        }
    }

    /// <summary>What one empty slot costs the player, phrased for a console line instead of a code comment.</summary>
    static string MenuSlotConsequence(string slot)
    {
        switch (slot)
        {
            case "playerUI": return "a menu opens with the HUD still on screen";
            case "PauseMenu": return "the pause key does nothing at all";
            case "AblityMenu": return "the ability menu key does nothing at all";
            case "OptionsMenu": return "the options menu cannot be opened or stepped back out of";
            case "DeathMenu": return "the death screen never appears, and only an animation event asks for it";
            default: return "the menu path that needs it is skipped";
        }
    }

    /// <summary>
    /// The script GUID of PlayerUIController, found by name instead of hardcoded: move or rename the file and this
    /// follows it, and when nothing matches the check says so rather than quietly checking nothing. A MonoScript's
    /// own GUID is exactly what a component's m_Script line stores, so no type reference is needed here.
    /// </summary>
    static string MenuScriptGuid(List<Issue> issues)
    {
        const string scriptName = "PlayerUIController";
        var guids = AssetDatabase.FindAssets(scriptName + " t:MonoScript");
        for (var i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (IsExcluded(path)) continue;
            if (string.Equals(Path.GetFileNameWithoutExtension(path), scriptName, StringComparison.Ordinal))
                return guids[i];
        }

        if (issues != null)
            Add(issues, "check 9 found no first-party script asset named " + scriptName + ", so no scene's menu "
                + "slots were checked (T-056). If the component was renamed, update AbilityReferenceValidator.MenuSlots "
                + "and the name this looks for; if it was deleted, delete the check too.", null);
        return null;
    }

    /// <summary>
    /// Every prefab carrying a PlayerUIController, with the slot values it ships: those are the defaults a scene's
    /// own modifications start from, which is why they have to be read before any scene can be judged.
    /// <para>
    /// The guid is searched in the raw text first, then the block is parsed. Walking every line of ~350 prefabs
    /// to find one component cost 1.5 s of the pass (measured 11/16/2026), which is most of the validator's warm
    /// budget for a check that exists because of one prefab.
    /// </para>
    /// </summary>
    static void CollectMenuSlotSources(string scriptGuid, List<MenuSlotSource> sources)
    {
        var dataPath = Application.dataPath.Replace('\\', '/');
        foreach (var file in Directory.GetFiles(Application.dataPath, "*.prefab", SearchOption.AllDirectories))
        {
            var path = "Assets" + file.Replace('\\', '/').Substring(dataPath.Length);
            if (IsExcluded(path)) continue;

            var text = TryReadAllText(file);
            if (text == null || text.IndexOf(scriptGuid, StringComparison.Ordinal) < 0) continue;

            var blocks = ReadComponentBlocks(file, scriptGuid);
            if (blocks.Count == 0) continue;

            var guid = AssetDatabase.AssetPathToGUID(path);
            for (var i = 0; i < blocks.Count; i++)
                sources.Add(new MenuSlotSource { PrefabPath = path, PrefabGuid = guid, Block = blocks[i] });
        }
    }

    /// <summary>Null for a file we cannot read - an unreadable file is not a finding, to this check or any other.</summary>
    static string TryReadAllText(string file)
    {
        try
        {
            return File.ReadAllText(file);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Every component block of one script in a prefab file. Blocks are delimited by their
    /// "--- !u!&lt;class&gt; &amp;&lt;fileID&gt;" header and a MonoBehaviour's serialized fields always follow its own
    /// m_Script line inside the same block, so five flags is all this check needs from one read of the file.
    /// Only prefabs come through here since the single-pass rewrite: a scene's components (none exist for this
    /// check today) and its prefab-instance overrides are read together in ScanMenuSlotInstances.
    /// </summary>
    static List<ComponentBlock> ReadComponentBlocks(string file, string scriptGuid)
    {
        var blocks = new List<ComponentBlock>();
        long blockID = 0;
        ComponentBlock current = null;
        try
        {
            using (var reader = new StreamReader(file))
            {
                string line;
                var lineNo = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNo++;
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("--- !u!", StringComparison.Ordinal))
                    {
                        blockID = BlockIDOf(trimmed);
                        current = null;
                        continue;
                    }

                    if (current == null)
                    {
                        if (trimmed.StartsWith("m_Script:", StringComparison.Ordinal)
                            && trimmed.IndexOf(scriptGuid, StringComparison.Ordinal) >= 0)
                        {
                            current = new ComponentBlock { BlockID = blockID, Line = lineNo };
                            blocks.Add(current);
                        }
                        continue;
                    }

                    var slot = SlotIndexOfField(trimmed);
                    if (slot >= 0) current.Assigned[slot] = ReferenceIsSet(trimmed);
                }
            }
        }
        catch (Exception)
        {
            // A file we cannot read is not a finding; every other check simply does not see it either.
        }
        return blocks;
    }

    /// <summary>
    /// The PlayerUIController instances of one scene as the running scene sees them: anything placed in it
    /// directly, plus one entry per Player prefab instance carrying the prefab defaults and then that instance's
    /// own modifications, last write wins - the order Unity's own deserializer uses.
    /// <para>
    /// ONE pass over the file. The first version read each scene twice (a component-block pass and a
    /// modification pass) and cost ~1.1 s across the built scenes that carry a Player, on files up to 23 MB
    /// (FirstForest.unity, measured 11/16/2026). Outside the two shapes this check cares about a line is only
    /// ever tested for the marker that opens one, in the IndexOf style the event-binding scan in this file uses.
    /// </para>
    /// </summary>
    static List<MenuSlotInstance> ScanMenuSlotInstances(string path, string scriptGuid, List<MenuSlotSource> sources)
    {
        var found = new List<MenuSlotInstance>();
        MenuSlotInstance direct = null;    // a component block of the script placed straight into the scene
        MenuSlotInstance instance = null;  // the prefab instance whose modification list is being read
        var inMods = false;
        var pendingSlot = -1;

        try
        {
            using (var reader = new StreamReader(path))
            {
                string line;
                var lineNo = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNo++;

                    // A component block never spans documents, and a modification list never survives one either.
                    if (line.StartsWith("--- !u!", StringComparison.Ordinal))
                    {
                        if (instance != null) { found.Add(instance); instance = null; }
                        direct = null;
                        inMods = false;
                        pendingSlot = -1;
                        continue;
                    }

                    if (direct != null)
                    {
                        var filled = SlotIndexOfField(line.TrimStart());
                        if (filled >= 0) direct.Assigned[filled] = ReferenceIsSet(line);
                        continue;
                    }

                    if (!inMods)
                    {
                        if (line.IndexOf("m_Modifications:", StringComparison.Ordinal) >= 0) inMods = true;
                        else if (line.IndexOf("m_Script:", StringComparison.Ordinal) >= 0
                                 && line.IndexOf(scriptGuid, StringComparison.Ordinal) >= 0)
                        {
                            direct = new MenuSlotInstance
                            {
                                OwnerPath = path,
                                Label = "the PlayerUIController placed in this scene",
                                Line = lineNo
                            };
                            found.Add(direct);
                        }
                        continue;
                    }

                    // m_Modifications: opens a prefab instance's override list; the m_Removed / m_Source lines that
                    // follow it close it. The singular "m_Modification:" key does NOT match, which is intended.
                    if (line.IndexOf("m_RemovedComponents:", StringComparison.Ordinal) >= 0
                        || line.IndexOf("m_SourcePrefab:", StringComparison.Ordinal) >= 0)
                    {
                        if (instance != null) found.Add(instance);
                        instance = null;
                        inMods = false;
                        pendingSlot = -1;
                        continue;
                    }

                    if (line.IndexOf("- target:", StringComparison.Ordinal) >= 0)
                    {
                        var source = MatchMenuSlotSource(sources, FileIDOf(line), GuidOf(line));
                        if (source != null && instance == null)
                        {
                            instance = new MenuSlotInstance
                            {
                                OwnerPath = path,
                                Label = "the Player instance in this scene",
                                Line = lineNo
                            };
                            for (var i = 0; i < MenuSlots.Length; i++) instance.Assigned[i] = source.Block.Assigned[i];
                        }
                        pendingSlot = -1;
                        continue;
                    }

                    if (instance == null) continue;

                    if (line.IndexOf("propertyPath:", StringComparison.Ordinal) >= 0)
                    {
                        pendingSlot = SlotOfName(After(line.Trim(), "propertyPath:"));
                        continue;
                    }
                    if (pendingSlot >= 0 && line.IndexOf("objectReference:", StringComparison.Ordinal) >= 0)
                    {
                        instance.Assigned[pendingSlot] = FileIDOf(line) != 0;
                        pendingSlot = -1;
                    }
                }
            }
        }
        catch (Exception)
        {
            // as above: an unreadable file is not a finding
        }

        // Every add above nulls the tracker, so anything still open here ran to the end of the file.
        if (instance != null) found.Add(instance);
        return found;
    }

    /// <summary>The modification target that belongs to a known PlayerUIController, or null.</summary>
    static MenuSlotSource MatchMenuSlotSource(List<MenuSlotSource> sources, long componentID, string prefabGuid)
    {
        if (componentID == 0 || string.IsNullOrEmpty(prefabGuid)) return null;
        for (var i = 0; i < sources.Count; i++)
        {
            if (sources[i].Block.BlockID == componentID
                && string.Equals(sources[i].PrefabGuid, prefabGuid, StringComparison.Ordinal)) return sources[i];
        }
        return null;
    }

    /// <summary>Which slot a serialized field line ("DeathMenu: {fileID: 0}") names, or -1.</summary>
    static int SlotIndexOfField(string trimmedLine)
    {
        for (var i = 0; i < MenuSlots.Length; i++)
        {
            if (trimmedLine.StartsWith(MenuSlots[i] + ":", StringComparison.Ordinal)) return i;
        }
        return -1;
    }

    /// <summary>Which slot a propertyPath names, matched exactly: a nested path is not the slot's own reference.</summary>
    static int SlotOfName(string name)
    {
        for (var i = 0; i < MenuSlots.Length; i++)
        {
            if (name == MenuSlots[i]) return i;
        }
        return -1;
    }

    /// <summary>True when a line carries a reference that points at something, i.e. its fileID is not 0.</summary>
    static bool ReferenceIsSet(string line)
    {
        return FileIDOf(line) != 0;
    }

    /// <summary>The &amp; number of a "--- !u!114 &8360464295297684396" document header.</summary>
    static long BlockIDOf(string header)
    {
        var at = header.IndexOf('&');
        return at < 0 ? 0 : DigitsAfter(header, at + 1);
    }

    /// <summary>The fileID of a "{fileID: 942453136}" reference, 0 when the reference points at nothing.</summary>
    static long FileIDOf(string text)
    {
        var at = text.IndexOf("fileID:", StringComparison.Ordinal);
        return at < 0 ? 0 : DigitsAfter(text, at + "fileID:".Length);
    }

    static long DigitsAfter(string text, int from)
    {
        from = SkipSpaces(text, from);
        var end = from;
        while (end < text.Length && (char.IsDigit(text[end]) || text[end] == '-')) end++;
        if (end == from) return 0;
        long value;
        return long.TryParse(text.Substring(from, end - from), out value) ? value : 0;
    }

    /// <summary>
    /// The guid value of a "- target: {fileID: 1, guid: abc, type: 3}" line. YAML writes a space after the colon,
    /// so the value does not start on the character right after the key - that space is what made the first
    /// version of this parser read every reference as empty (see T-056's report: a check that sees nothing prints
    /// the same summary as a project with nothing wrong).
    /// </summary>
    static string GuidOf(string text)
    {
        var at = text.IndexOf("guid:", StringComparison.Ordinal);
        if (at < 0) return "";
        at = SkipSpaces(text, at + "guid:".Length);
        var end = at;
        while (end < text.Length && char.IsLetterOrDigit(text[end])) end++;
        return text.Substring(at, end - at);
    }

    /// <summary>YAML separators: the space between "fileID:" and its number inside a flow mapping.</summary>
    static int SkipSpaces(string text, int from)
    {
        while (from < text.Length && (text[from] == ' ' || text[from] == '\t')) from++;
        return from;
    }

    /// <summary>
    /// Every PlayerUIController in the project with its five menu slots as they will read at runtime, built scenes
    /// and scratch scenes alike, so check 9's "only built scenes are reported" scope is a stated scope and never a
    /// hiding place. Read-only, same shape as Tools/Ciro/Print Singleton Debt.
    /// </summary>
    [MenuItem("Tools/Ciro/Print Menu Slots")]
    public static void PrintMenuSlots()
    {
        var scriptGuid = MenuScriptGuid(null);
        if (scriptGuid == null)
        {
            Debug.LogWarning(Tag + "cannot resolve the PlayerUIController script GUID, so menu slots cannot be listed.");
            return;
        }

        var sources = new List<MenuSlotSource>();
        CollectMenuSlotSources(scriptGuid, sources);

        var built = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var scenes = EditorBuildSettings.scenes;
        for (var s = 0; s < scenes.Length; s++) if (scenes[s].enabled) built.Add(scenes[s].path);

        for (var i = 0; i < sources.Count; i++)
        {
            Debug.Log(Tag + "prefab defaults   " + sources[i].PrefabPath + ":" + sources[i].Block.Line + "   "
                + SlotTable(sources[i].Block.Assigned)
                + "   (empty here is by design: every scene overrides the five on its own Player instance)");
        }

        var dataPath = Application.dataPath.Replace('\\', '/');
        foreach (var file in Directory.GetFiles(Application.dataPath, "*.unity", SearchOption.AllDirectories))
        {
            var path = "Assets" + file.Replace('\\', '/').Substring(dataPath.Length);
            if (IsExcluded(path)) continue;

            var instances = ScanMenuSlotInstances(path, scriptGuid, sources);
            for (var i = 0; i < instances.Count; i++)
            {
                var instance = instances[i];
                Debug.Log(Tag + path + ":" + instance.Line + "   ["
                    + (built.Contains(path) ? "in build" : "NOT in build - check 9 stays quiet") + "]   "
                    + SlotTable(instance.Assigned));
            }
        }
    }

    /// <summary>One column per menu slot: x = wired up, - = empty.</summary>
    static string SlotTable(bool[] assigned)
    {
        var parts = new string[MenuSlots.Length];
        for (var i = 0; i < MenuSlots.Length; i++) parts[i] = (assigned[i] ? "x " : "- ") + MenuSlots[i];
        return string.Join("  ", parts);
    }

    static int LineOf(string text, int index)
    {
        var line = 1;
        for (var i = 0; i < index && i < text.Length; i++)
        {
            if (text[i] == '\n') line++;
        }
        return line;
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
