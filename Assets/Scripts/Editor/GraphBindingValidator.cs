using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;
using Object = UnityEngine.Object;

/// <summary>
/// T-055 guard rail: a Behavior Graph binds a node to its C# members BY FIELD NAME.
///
/// A node's fields live in the graph .asset as [SerializeReference] managed references, and the
/// key Unity writes for each of them is the C# FIELD NAME (AlertGroupAction's fields arrive as the
/// keys "Self" and "GroupController" inside the record "type: {class: AlertGroupAction, ...}").
/// Rename the field, or the type it closes over (BlackboardVariable&lt;T&gt;), and Unity has nowhere
/// to put the old key: it silently drops the binding. No compile error, no import error, no
/// validator problem. The field arrives null, and the first symptom is a NullReferenceException on
/// "SomeField.Value" in the middle of a fight, days after the rename that caused it.
///
/// Proof from this project: commit 574a7847 (T-026 identifier typo sweep) renamed the field
/// "EnemyGroupContoller" -> "GroupController" on AlertGroupAction. The sweep rewrote the TYPE string
/// (EnemiesGroupContoller -> EnemiesGroupController) everywhere, including the graph assets, but the
/// FIELD key stayed "EnemyGroupContoller" in all 6 enemy graphs - so GroupController was null on
/// 44 of 44 live enemy nodes, which is the crash T-037 spent five days chasing.
///
/// What this validator checks, in Edit mode, from the .asset text plus the loaded assemblies:
///   1. a node record carries a key that is not a field of its C# type   -> the binding is dropped
///   2. a node's BlackboardVariable field has no key in the record        -> it arrives null
///   3. a managed reference type in the asset no longer resolves          -> a type was renamed away
///   4. the graph editor's cached node definition lists a variable that is not a field of its node
///      class (this is what the Behavior Graph window would write back)
///   5. Unity-Behavior's own "I dropped a managed reference" flags in the asset
///
/// It reads files and the loaded assembly only: it never opens, modifies, re-serialises or saves an
/// asset. It never throws; a failure to parse is reported, not swallowed silently.
/// Runs from Tools/Ciro/Validate Graph Node Bindings. Unlike Tools/Ciro/Validate Ability References
/// it does NOT run by itself after a script reload: while a known-broken binding is still in the
/// project it would put red text in the console on every reload, and the fix is a designer edit in
/// the Behavior Graph window. Add an [InitializeOnLoadMethod] one-liner here once the count is clean.
/// </summary>
public static class GraphBindingValidator
{
    const string Tag = "[GraphBindingValidator] ";

    /// <summary>The assembly first-party scripts compile into (the project has no asmdefs outside Synty).</summary>
    const string FirstPartyAsm = "Assembly-CSharp";

    const string NodeTypeName = "Unity.Behavior.Node";
    const string BlackboardVariableName = "Unity.Behavior.BlackboardVariable";
    const string AgentTypeName = "Unity.Behavior.BehaviorGraphAgent";

    /// Third-party copies and leftovers: never scanned (the list AbilityReferenceValidator uses).
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

    /// <summary>One "- rid: N / type: {class:.., ns:.., asm:..} / data:" block of a references list.</summary>
    class Record
    {
        public long Rid;
        public int Line;
        public string ClassName = "";
        public string Namespace = "";
        public string Assembly = "";
        public bool HasType;
        public bool InData;
        /// <summary>Field-name key -> the line it was written on (first occurrence).</summary>
        public Dictionary<string, int> Keys = new Dictionary<string, int>(StringComparer.Ordinal);

        public string FullType
        {
            get { return string.IsNullOrEmpty(Namespace) ? ClassName : Namespace + "." + ClassName; }
        }
    }

    /// <summary>One entry of the graph editor's cached node-definition list.</summary>
    class NodeDef
    {
        public string Name = "";
        public int NameLine;
        public string RuntimeTypeString = "";
        public int TypeLine;
        public List<string> VarNames = new List<string>();
        public List<string> VarTypes = new List<string>();
        public List<int> VarLines = new List<int>();
    }

    class Counts
    {
        public int graphs;
        public int records;
        public int nodeRecords;
        public int serializedFields;
        public int nodeDefs;
        public int defVariables;
    }

    [MenuItem("Tools/Ciro/Validate Graph Node Bindings")]
    public static void ValidateMenuItem()
    {
        Validate();
    }

    /// <summary>
    /// Runs every static check and logs the result. Returns problems + warnings as ONE number
    /// (same trap as AbilityReferenceValidator: read the summary line, not the bare return value).
    /// </summary>
    public static int Validate()
    {
        var sw = Stopwatch.StartNew();
        var problems = new List<Issue>();
        var warnings = new List<Issue>();
        var counts = new Counts();

        int selfTest = RunSelfTest();
        if (selfTest != 0)
        {
            Debug.LogError(Tag + "the validator's own self-test failed (" + selfTest
                + " case(s)), so a green result here would be meaningless. Fix the self-test first.");
            return selfTest;
        }

        foreach (var path in FindAssets("t:BehaviorAuthoringGraph"))
        {
            counts.graphs++;
            try
            {
                CheckGraph(path, problems, warnings, counts);
            }
            catch (Exception e)
            {
                Add(problems, path + " could not be checked (" + e.GetType().Name + ": " + e.Message
                    + "), its bindings were NOT verified.", null);
            }
        }

        sw.Stop();
        foreach (var issue in problems) Debug.LogError(Tag + issue.Message, issue.Context);
        foreach (var warning in warnings) Debug.LogWarning(Tag + warning.Message, warning.Context);

        var summary = Tag + "checked " + counts.graphs + " behavior graphs (" + counts.records
            + " serialized records, " + counts.nodeRecords + " node records, " + counts.serializedFields
            + " serialized fields, " + counts.nodeDefs + " cached node definitions with "
            + counts.defVariables + " variables) in " + sw.ElapsedMilliseconds + "ms";
        if (problems.Count == 0 && warnings.Count == 0)
            Debug.Log(summary + " -> OK, every node binding resolves and every key has a field.");
        else
            Debug.LogWarning(summary + " -> " + problems.Count + " problem(s) and " + warnings.Count
                + " warning(s) logged above: a [SerializeReference] binding is being dropped silently.");
        return problems.Count + warnings.Count;
    }

    // ------------------------------------------------------------------ one graph asset
    static void CheckGraph(string path, List<Issue> problems, List<Issue> warnings, Counts counts)
    {
        List<Record> records;
        List<NodeDef> defs;
        int[] flags;
        ParseAsset(path, out records, out defs, out flags);

        var context = AssetDatabase.LoadAssetAtPath<Object>(path);

        // 5 - Unity-Behavior's own markers: the asset records that a managed reference was dropped
        // while loading, which is exactly the silent case this tool exists to catch.
        if (flags[0] != 0) Add(problems, path + ": Unity reports m_GraphMissingManagedRef - a node reference was dropped when this graph was loaded.", context);
        if (flags[1] != 0) Add(problems, path + ": Unity reports m_BlackboardMissingManagedRef - a blackboard variable reference was dropped when this graph was loaded.", context);
        if (flags[2] != 0) Add(problems, path + ": Unity reports m_HasMissingTypeInManagedRef - a managed reference type in this asset no longer exists.", context);

        // Buckets keyed by "node type.member", so a graph that repeats one broken node 44 times
        // logs one line instead of 44.
        var staleFirstParty = new Dictionary<string, string>(StringComparer.Ordinal);
        var staleThirdParty = new Dictionary<string, string>(StringComparer.Ordinal);
        var missingBinding = new Dictionary<string, string>(StringComparer.Ordinal);
        var typeCache = new Dictionary<string, Type>(StringComparer.Ordinal);
        var fieldCache = new Dictionary<Type, HashSet<string>>();

        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            if (!record.HasType || string.IsNullOrEmpty(record.ClassName)) continue; // the "- rid: -2" sentinel

            counts.records++;
            var type = ResolveCached(record.FullType, record.Assembly, typeCache);

            // 3 - the managed reference type itself is gone: everything pointing at it is null.
            if (type == null)
            {
                Add(problems, path + ":" + record.Line + " a managed reference has the type " + Q(record.FullType)
                    + " in assembly " + Q(record.Assembly) + ", which no longer exists"
                    + MissingArgumentHint(record.FullType)
                    + ". Whatever pointed at it deserialized as null - restore the name or re-bind it in the Behavior Graph window.",
                    context);
                continue;
            }

            if (record.Keys.Count == 0) continue;

            bool firstParty = string.Equals(record.Assembly, FirstPartyAsm, StringComparison.Ordinal);
            if (IsNodeType(type)) counts.nodeRecords++;

            var fields = FieldNames(type, fieldCache);
            counts.serializedFields += record.Keys.Count;

            // 1 - a key with no field: the C# member was renamed or deleted, so Unity drops the value.
            foreach (var pair in record.Keys)
            {
                if (fields.Contains(pair.Key)) continue;
                string bucketKey = record.FullType + "." + pair.Key;
                string detail = path + ":" + pair.Value + " " + Short(type) + " is serialized with the field key " + Q(pair.Key)
                    + ", which no longer exists on that type";
                string lookalike = ClosestField(type, pair.Key);
                if (lookalike != null) detail += " - the class now has " + Q(lookalike) + ", so the member was renamed";
                if (firstParty)
                {
                    if (!staleFirstParty.ContainsKey(bucketKey))
                        staleFirstParty[bucketKey] = detail + ", and the value is dropped: that member is null at runtime. Re-bind it in the Behavior Graph window and save the graph.";
                }
                else
                {
                    if (!staleThirdParty.ContainsKey(bucketKey))
                        staleThirdParty[bucketKey] = detail + " (third-party type: a package upgrade is the usual cause, not a project rename).";
                }
            }

            // 2 - a binding the C# class declares that this record never wrote: null at runtime.
            if (firstParty && IsNodeType(type))
            {
                foreach (var f in BindingFields(type))
                {
                    if (record.Keys.ContainsKey(f.Name)) continue;
                    string bucketKey = record.FullType + "." + f.Name;
                    if (missingBinding.ContainsKey(bucketKey)) continue;
                    missingBinding[bucketKey] = path + ":" + record.Line + " " + Short(type)
                        + " has no serialized data for " + Q(f.Name) + " (" + Short(f.FieldType) + "). It arrives null, so "
                        + Q(f.Name + ".Value") + " throws the first time the node runs. Connect the blackboard variable in the Behavior Graph window, or guard the field in code.";
                }
            }
        }

        foreach (var pair in staleFirstParty) Add(problems, pair.Value, context);
        foreach (var pair in staleThirdParty) Add(warnings, pair.Value, context);
        foreach (var pair in missingBinding)
        {
            // Say how many nodes of that type are affected, so the size of the re-bind is obvious.
            int affected = 0;
            string member = pair.Key.Substring(pair.Key.LastIndexOf('.') + 1);
            string typeName = pair.Key.Substring(0, pair.Key.LastIndexOf('.'));
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].FullType != typeName) continue;
                if (!records[i].Keys.ContainsKey(member)) affected++;
            }
            Add(problems, affected + " node(s) - " + pair.Value, context);
        }

        CheckNodeDefinitions(path, defs, problems, counts);
    }

    /// <summary>
    /// Check 4: the Behavior Graph window caches, inside the authoring asset, the list of node types
    /// it offers and the variables each one exposes. That cache is only refreshed when a graph is
    /// opened or saved, so after a rename it keeps advertising the OLD member name - which is what
    /// the editor would write back into every node the next time the graph is saved.
    /// </summary>
    static void CheckNodeDefinitions(string path, List<NodeDef> defs, List<Issue> problems, Counts counts)
    {
        var context = AssetDatabase.LoadAssetAtPath<Object>(path);
        var typeCache = new Dictionary<string, Type>(StringComparer.Ordinal);
        var fieldCache = new Dictionary<Type, HashSet<string>>();

        for (int i = 0; i < defs.Count; i++)
        {
            var def = defs[i];
            counts.nodeDefs++;

            string fullName, assembly;
            if (!SplitSerializableType(def.RuntimeTypeString, out fullName, out assembly))
            {
                if (!string.IsNullOrEmpty(def.RuntimeTypeString))
                    Add(problems, path + ":" + def.TypeLine + " the cached node definition " + Q(def.Name) + " has the unreadable runtime type "
                        + Q(def.RuntimeTypeString) + ".", context);
                continue;
            }

            var type = ResolveCached(fullName, assembly, typeCache);
            if (type == null)
            {
                Add(problems, path + ":" + def.TypeLine + " the cached node definition " + Q(def.Name) + " points at the runtime type "
                    + Q(fullName) + " in assembly " + Q(assembly) + ", which no longer exists" + MissingArgumentHint(fullName)
                    + ". Every node of this type in the graph is null at runtime.", context);
                continue;
            }

            if (!string.Equals(assembly, FirstPartyAsm, StringComparison.Ordinal)) continue;

            var fields = FieldNames(type, fieldCache);
            for (int v = 0; v < def.VarNames.Count; v++)
            {
                counts.defVariables++;
                string varName = def.VarNames[v];
                if (fields.Contains(varName)) continue;

                string detail = path + ":" + def.VarLines[v] + " the cached node definition " + Q(def.Name) + " still lists the variable "
                    + Q(varName) + ", which is not a member of " + Short(type);
                string lookalike = ClosestField(type, varName);
                if (lookalike != null) detail += " - the class now has " + Q(lookalike);
                detail += ". Open the graph in the Behavior Graph window, re-bind the node and save it: the definition is refreshed on save.";
                Add(problems, detail, context);
            }

            for (int v = 0; v < def.VarTypes.Count; v++)
            {
                string varFull, varAsm;
                if (!SplitSerializableType(def.VarTypes[v], out varFull, out varAsm)) continue;
                if (ResolveCached(varFull, varAsm, typeCache) != null) continue;
                Add(problems, path + ":" + def.VarLines[v] + " the cached node definition " + Q(def.Name) + " declares variable "
                    + Q(def.VarNames[v]) + " with the type " + Q(varFull) + ", which no longer exists"
                    + MissingArgumentHint(varFull) + ".", context);
            }
        }
    }

    // ------------------------------------------------------------------ the .asset reader
    /// <summary>
    /// Reads a graph .asset as text and returns three things: every managed-reference record
    /// ("- rid: / type: {class:.., ns:.., asm:..} / data: <field keys>"), every entry of the graph
    /// editor's cached node model list (m_NodeModelsInfo), and Unity's three drop-flag fields.
    /// Indentation is the structure here, and Unity writes it with spaces only.
    /// </summary>
    static void ParseAsset(string path, out List<Record> records, out List<NodeDef> defs, out int[] flags)
    {
        records = new List<Record>();
        defs = new List<NodeDef>();
        flags = new int[3];

        Record current = null;
        NodeDef currentDef = null;
        bool inReferences = false;
        bool inNodeModels = false;
        bool inVariables = false;

        int lineNo = 0;
        foreach (var rawLine in File.ReadLines(path))
        {
            lineNo++;
            var line = rawLine.EndsWith("\r") ? rawLine.Substring(0, rawLine.Length - 1) : rawLine;
            if (line.Length == 0) continue;

            int indent = 0;
            while (indent < line.Length && line[indent] == ' ') indent++;
            var text = line.Substring(indent);

            if (indent == 0) // a new document of the asset ends every open block
            {
                current = null;
                currentDef = null;
                inReferences = false;
                inNodeModels = false;
                inVariables = false;
                continue;
            }

            if (indent == 2)
            {
                if (text.StartsWith("m_GraphMissingManagedRef:")) { flags[0] = ParseCount(text); continue; }
                if (text.StartsWith("m_BlackboardMissingManagedRef:")) { flags[1] = ParseCount(text); continue; }
                if (text.StartsWith("m_HasMissingTypeInManagedRef:")) { flags[2] = ParseCount(text); continue; }
                if (text.StartsWith("references:")) { inReferences = true; continue; }
                if (text.StartsWith("m_NodeModelsInfo:"))
                {
                    inNodeModels = !text.EndsWith("[]");
                    currentDef = null;
                    inVariables = false;
                    continue;
                }
                if (inNodeModels)
                {
                    if (text.StartsWith("- Name: "))
                    {
                        currentDef = new NodeDef { Name = Unquote(text.Substring(8).Trim()), NameLine = lineNo };
                        defs.Add(currentDef);
                        inVariables = false;
                        continue;
                    }
                    inNodeModels = false; // the next indent-2 key closes the model list
                    continue;
                }
            }

            if (inNodeModels && currentDef != null)
            {
                if (indent == 4)
                {
                    if (text.StartsWith("- Name: ")) // a variable of the current model
                    {
                        currentDef.VarNames.Add(Unquote(text.Substring(8).Trim()));
                        currentDef.VarTypes.Add("");
                        currentDef.VarLines.Add(lineNo);
                        inVariables = true;
                        continue;
                    }
                    if (text.StartsWith("RuntimeTypeString:"))
                    {
                        currentDef.RuntimeTypeString = Unquote(text.Substring(18).Trim());
                        currentDef.TypeLine = lineNo;
                        inVariables = false;
                        continue;
                    }
                    if (text.StartsWith("Variables:"))
                    {
                        inVariables = !text.EndsWith("[]");
                        continue;
                    }
                    inVariables = false; // Story / RuntimeTypeID / NamedChildren / IsPlaceholder
                    continue;
                }
                if (inVariables && indent == 8 && text.StartsWith("m_SerializableType:"))
                {
                    int at = currentDef.VarTypes.Count - 1;
                    if (at >= 0) currentDef.VarTypes[at] = Unquote(text.Substring(19).Trim());
                    continue;
                }
                continue; // indent 5-7 belongs to the variable's own Type / Tooltip block
            }

            if (!inReferences) continue;

            if (indent == 4 && text.StartsWith("- rid:"))
            {
                current = new Record { Rid = ParseRid(text), Line = lineNo };
                records.Add(current);
                continue;
            }
            if (current == null) continue;

            if (indent == 6)
            {
                if (text.StartsWith("type:")) { ParseTypeLine(text, current); continue; }
                if (text == "data:") { current.InData = true; continue; }
                current.InData = false;
                continue;
            }

            if (current.InData && indent >= 8)
            {
                if (indent != 8 || text[0] == '-') continue; // a nested value, or a list item of a field
                int colon = text.IndexOf(':');
                if (colon <= 0) continue;
                var key = text.Substring(0, colon).Trim();
                if (!current.Keys.ContainsKey(key)) current.Keys[key] = lineNo;
            }
        }
    }

    // ------------------------------------------------------------------ types and fields
    static Type ResolveCached(string fullName, string assembly, Dictionary<string, Type> cache)
    {
        var key = (fullName ?? "") + "|" + (assembly ?? "");
        Type hit;
        if (cache.TryGetValue(key, out hit)) return hit;
        var found = Resolve(fullName, assembly);
        cache[key] = found;
        return found;
    }

    /// <summary>
    /// Resolves a name out of a graph asset: "AlertGroupAction" + "Assembly-CSharp", or a closed
    /// generic like "Unity.Behavior.BlackboardVariable`1[[EnemiesGroupController, Assembly-CSharp]]".
    /// Returns null when the type is gone, which is what a rename does to a managed reference.
    /// </summary>
    static Type Resolve(string fullName, string assembly)
    {
        if (string.IsNullOrEmpty(fullName)) return null;
        fullName = fullName.Trim().Replace('/', '+');

        Type found = null;
        try { found = Type.GetType(string.IsNullOrEmpty(assembly) ? fullName : fullName + ", " + assembly, false); }
        catch (Exception) { found = null; }
        if (found != null) return found;

        try { found = Type.GetType(fullName, false); }
        catch (Exception) { found = null; }
        if (found != null) return found;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            if (!string.IsNullOrEmpty(assembly) && assemblies[i].GetName().Name != assembly) continue;
            try { found = assemblies[i].GetType(fullName, false); }
            catch (Exception) { found = null; }
            if (found != null) return found;
        }

        // A closed generic can fail on its own bookkeeping (a version inside the argument's
        // assembly qualification). If the definition and every argument resolve, the type is
        // present: this check exists to catch names that are GONE, and MissingArgument names those.
        var cut = fullName.IndexOf("[[", StringComparison.Ordinal);
        if (cut > 0 && MissingArgument(fullName) == null) return Resolve(fullName.Substring(0, cut), assembly);
        return null;
    }

    /// <summary>
    /// The first generic argument of a closed generic name that no longer resolves, or null when
    /// every one of them does. This is the line that names the renamed type, e.g. EnemiesGroupContoller.
    /// </summary>
    static string MissingArgument(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return null;
        var start = fullName.IndexOf("[[", StringComparison.Ordinal);
        var end = fullName.LastIndexOf("]]", StringComparison.Ordinal);
        if (start < 0 || end <= start) return null;

        // Inside "X`1[[A, asm]]" - or "X`2[[A, asm],[B, asm]]" - the arguments are separated by "],[".
        var inner = fullName.Substring(start + 2, end - start - 2);
        var args = SplitGenericArgs(inner);
        for (int i = 0; i < args.Count; i++)
        {
            string argName, argAsm;
            if (!SplitSerializableType(args[i], out argName, out argAsm)) continue;
            if (Resolve(argName, argAsm) != null) continue;
            string deeper = MissingArgument(argName);
            return deeper ?? argName;
        }
        return null;
    }

    /// <summary>Suffix for a message: names the argument that vanished, when there is one.</summary>
    static string MissingArgumentHint(string fullName)
    {
        var missing = MissingArgument(fullName);
        return missing == null ? "" : " - the type inside it, " + Q(missing) + ", is what disappeared";
    }

    static Type graphNodeType;
    static bool graphNodeTypeChecked;

    static bool IsNodeType(Type t)
    {
        if (t == null) return false;
        if (!graphNodeTypeChecked)
        {
            graphNodeTypeChecked = true;
            graphNodeType = Resolve(NodeTypeName, "Unity.Behavior");
        }
        if (graphNodeType != null) return graphNodeType.IsAssignableFrom(t);
        return HasBindingFields(t); // package not loaded: judge by shape instead
    }

    /// <summary>Every instance field name Unity could write as a key, inherited levels included.</summary>
    static HashSet<string> FieldNames(Type t, Dictionary<Type, HashSet<string>> cache)
    {
        HashSet<string> known;
        if (cache != null && cache.TryGetValue(t, out known)) return known;

        known = new HashSet<string>(StringComparer.Ordinal);
        var level = t;
        while (level != null && level != typeof(object))
        {
            var fields = level.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            for (int i = 0; i < fields.Length; i++)
            {
                known.Add(fields[i].Name);
                // "[field: SerializeField] public T Foo" is written "<Foo>k__BackingField": accept both.
                if (fields[i].Name.StartsWith("<", StringComparison.Ordinal))
                {
                    var close = fields[i].Name.IndexOf('>');
                    if (close > 1) known.Add(fields[i].Name.Substring(1, close - 1));
                }
                var attrs = fields[i].GetCustomAttributesData();
                for (int a = 0; a < attrs.Count; a++)
                {
                    if (attrs[a].AttributeType.Name != "FormerlySerializedAsAttribute" || attrs[a].ConstructorArguments.Count == 0) continue;
                    var old = attrs[a].ConstructorArguments[0].Value as string;
                    if (!string.IsNullOrEmpty(old)) known.Add(old);
                }
            }
            level = level.BaseType;
        }
        if (cache != null) cache[t] = known;
        return known;
    }

    static readonly Dictionary<Type, List<FieldInfo>> BindingFieldCache = new Dictionary<Type, List<FieldInfo>>();

    /// <summary>
    /// The BlackboardVariable members of a node type - the bindings a graph has to fill in. Public
    /// or [SerializeField]/[SerializeReference], which is exactly what Unity writes a key for.
    /// </summary>
    static List<FieldInfo> BindingFields(Type t)
    {
        List<FieldInfo> cached;
        if (BindingFieldCache.TryGetValue(t, out cached)) return cached;

        var list = new List<FieldInfo>();
        var level = t;
        while (level != null && level != typeof(object))
        {
            var fields = level.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];
                if (!IsBlackboardVariable(f.FieldType)) continue;
                if (f.IsStatic) continue;
                if (!f.IsPublic && !HasAttribute(f, "SerializeField") && !HasAttribute(f, "SerializeReference")) continue;
                if (HasAttribute(f, "NonSerialized")) continue;
                list.Add(f);
            }
            level = level.BaseType;
        }
        BindingFieldCache[t] = list;
        return list;
    }

    static bool HasBindingFields(Type t)
    {
        return BindingFields(t).Count > 0;
    }

    static bool IsBlackboardVariable(Type t)
    {
        var level = t;
        while (level != null)
        {
            var name = level.FullName ?? level.Name;
            if (name.StartsWith(BlackboardVariableName, StringComparison.Ordinal)) return true;
            level = level.BaseType;
        }
        return false;
    }

    static bool HasAttribute(FieldInfo f, string name)
    {
        var attrs = f.GetCustomAttributesData();
        for (int i = 0; i < attrs.Count; i++)
        {
            var attributeName = attrs[i].AttributeType.Name;
            if (attributeName == name || attributeName == name + "Attribute") return true;
        }
        return false;
    }

    /// <summary>
    /// The member that a stale key most likely used to be, so the message can say "it was probably
    /// renamed to X" instead of leaving the reader to diff the commit by hand.
    /// </summary>
    static string ClosestField(Type t, string key)
    {
        if (t == null || string.IsNullOrEmpty(key)) return null;
        var level = t;
        while (level != null && level != typeof(object))
        {
            var fields = level.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            for (int i = 0; i < fields.Length; i++)
                if (string.Equals(fields[i].Name, key, StringComparison.OrdinalIgnoreCase)) return fields[i].Name;
            level = level.BaseType;
        }

        level = t;
        while (level != null && level != typeof(object))
        {
            var fields = level.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            for (int i = 0; i < fields.Length; i++)
            {
                var name = fields[i].Name;
                if (name.Length < 4 || key.Length < 4) continue;
                if (name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0
                    || key.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return name;
            }
            level = level.BaseType;
        }
        return null;
    }

    // ------------------------------------------------------------------ readers and small helpers
    static void ParseTypeLine(string text, Record record)
    {
        var open = text.IndexOf('{');
        var close = text.LastIndexOf('}');
        if (open < 0 || close <= open) return;

        var parts = SplitTopLevel(text.Substring(open + 1, close - open - 1), ',');
        for (int i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            var colon = part.IndexOf(':');
            if (colon <= 0) continue;
            var name = part.Substring(0, colon).Trim();
            var value = Unquote(part.Substring(colon + 1).Trim());
            if (name == "class")
            {
                record.ClassName = value;
                record.HasType = value.Length > 0;
            }
            else if (name == "ns") record.Namespace = value;
            else if (name == "asm") record.Assembly = value;
        }
    }

    /// <summary>
    /// "AlertGroupAction, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"
    /// into the type name and the assembly name. Closed generics keep their arguments, because the
    /// argument is exactly the name a rename takes away.
    /// </summary>
    static bool SplitSerializableType(string value, out string fullName, out string assembly)
    {
        fullName = "";
        assembly = "";
        if (string.IsNullOrEmpty(value)) return false;
        var parts = SplitTopLevel(value, ',');
        if (parts.Count == 0 || parts[0].Length == 0) return false;
        fullName = Unquote(parts[0]);
        if (parts.Count > 1) assembly = Unquote(parts[1]);
        return true;
    }

    /// <summary>Splits on a separator that sits outside [...] brackets and outside '...' quotes.</summary>
    static List<string> SplitTopLevel(string text, char separator)
    {
        var parts = new List<string>();
        if (string.IsNullOrEmpty(text)) return parts;

        var current = new StringBuilder();
        int depth = 0;
        bool quoted = false;
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\'') quoted = !quoted;
            else if (!quoted && (c == '[' || c == '{' || c == '(')) depth++;
            else if (!quoted && (c == ']' || c == '}' || c == ')')) depth--;

            if (!quoted && depth == 0 && c == separator)
            {
                parts.Add(current.ToString().Trim());
                current.Length = 0;
                continue;
            }
            current.Append(c);
        }
        parts.Add(current.ToString().Trim());
        return parts;
    }

    /// <summary>The arguments of a closed generic: "X`1[[A, asm]]" or "X`2[[A, asm],[B, asm]]".</summary>
    static List<string> SplitGenericArgs(string inner)
    {
        var parts = new List<string>();
        if (string.IsNullOrEmpty(inner)) return parts;

        // Unity separates closed generic arguments with "],["; a nested generic inside one argument
        // ends with "]]," and so never produces that sequence by accident.
        int index = 0;
        while (true)
        {
            var at = inner.IndexOf("],[", index, StringComparison.Ordinal);
            if (at < 0)
            {
                parts.Add(Unquote(inner.Substring(index).Trim()));
                break;
            }
            parts.Add(Unquote(inner.Substring(index, at - index).Trim()));
            index = at + 3;
        }
        return parts;
    }






    static long ParseRid(string text)
    {
        var colon = text.IndexOf(':');
        long rid;
        return colon > 0 && long.TryParse(text.Substring(colon + 1).Trim(), out rid) ? rid : 0;
    }

    static int ParseCount(string text)
    {
        var colon = text.IndexOf(':');
        int value;
        return colon > 0 && int.TryParse(text.Substring(colon + 1).Trim(), out value) ? value : 0;
    }

    /// <summary>Strips the quotes YAML puts around a value that starts with a special character.</summary>
    static string Unquote(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Length > 1 && value[0] == '\'' && value[value.Length - 1] == '\'') return value.Substring(1, value.Length - 2);
        if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"') return value.Substring(1, value.Length - 2);
        return value;
    }

    static string Q(string value)
    {
        return value == null ? "<null>" : "'" + value + "'";
    }

    /// <summary>A type name short enough to read in a console line: BlackboardVariable&lt;EnemiesGroupController&gt;.</summary>
    static string Short(Type t)
    {
        if (t == null) return "<null type>";
        if (!t.IsGenericType) return t.FullName ?? t.Name;

        var name = t.Name;
        var tick = name.IndexOf('`');
        if (tick > 0) name = name.Substring(0, tick);

        var args = t.GetGenericArguments();
        var sb = new StringBuilder(name);
        sb.Append('<');
        for (int i = 0; i < args.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(args[i].IsGenericType ? Short(args[i]) : args[i].Name);
        }
        sb.Append('>');
        return sb.ToString();
    }

    static void Add(List<Issue> issues, string message, Object context)
    {
        issues.Add(new Issue { Message = message, Context = context });
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

    static bool IsExcluded(string path)
    {
        for (int i = 0; i < ExcludedPrefixes.Length; i++)
            if (path.StartsWith(ExcludedPrefixes[i], StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    // ------------------------------------------------------------------ live scan (play mode)
    /// <summary>
    /// The play-mode tier of the verification ladder: walks the node instances the running game
    /// actually built (agent graph -> module -> nodes) and reports the [SerializeReference]
    /// bindings that arrived null. The static pass predicts them from the asset; this one proves it.
    /// </summary>
    [MenuItem("Tools/Ciro/Validate Graph Node Bindings (Live Play Mode Scan)")]
    public static void LiveScanMenuItem()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning(Tag + "the live scan reads running graph instances: enter Play mode, load a level with agents, then run it again. "
                + "Tools/Ciro/Validate Graph Node Bindings is the Edit-mode pass and needs none of that.");
            return;
        }

        var sw = Stopwatch.StartNew();
        var agentType = Resolve(AgentTypeName, "Unity.Behavior");
        if (agentType == null)
        {
            Debug.LogError(Tag + "Unity.Behavior.BehaviorGraphAgent is not loaded, so there is nothing to scan.");
            return;
        }

        var agents = Object.FindObjectsByType(agentType, FindObjectsInactive.Include, FindObjectsSortMode.None);
        var graphProperty = agentType.GetProperty("Graph", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var nodeType = graphNodeType ?? (graphNodeType = Resolve(NodeTypeName, "Unity.Behavior"));

        var visited = new HashSet<object>();
        var nodes = new List<object>();
        var nulls = new Dictionary<string, string>(StringComparer.Ordinal);
        int firstPartyNodes = 0;
        int checkedFields = 0;

        for (int a = 0; a < agents.Length; a++)
        {
            var graph = graphProperty != null ? graphProperty.GetValue(agents[a], null) : null;
            if (graph == null) continue;
            nodes.Clear();
            CollectNodes(graph, nodeType, visited, nodes, 0);

            for (int n = 0; n < nodes.Count; n++)
            {
                var node = nodes[n];
                var type = node.GetType();
                if (type.Assembly.GetName().Name != FirstPartyAsm) continue;
                firstPartyNodes++;

                var bindings = BindingFields(type);
                for (int b = 0; b < bindings.Count; b++)
                {
                    checkedFields++;
                    object value = null;
                    try { value = bindings[b].GetValue(node); }
                    catch (Exception) { value = null; }
                    if (value != null) continue;

                    var key = type.FullName + "." + bindings[b].Name;
                    if (nulls.ContainsKey(key)) continue;
                    nulls[key] = "a live " + Short(type) + " node has a null " + Q(bindings[b].Name) + " binding ("
                        + DescribeOwner(node) + "). The graph never wrote that field: "
                        + "see Tools/Ciro/Validate Graph Node Bindings for the asset and line.";
                }
            }
        }

        sw.Stop();
        foreach (var pair in nulls) Debug.LogError(Tag + pair.Value);

        var line = Tag + "live scan read " + agents.Length + " agents and " + firstPartyNodes
            + " first-party node instances (" + checkedFields + " BlackboardVariable fields) in "
            + sw.ElapsedMilliseconds + "ms";
        if (agents.Length == 0)
            Debug.LogError(Tag + "no BehaviorGraphAgent is in this scene, so the scan proves nothing. Load a level with enemies or NPCs.");
        else if (firstPartyNodes == 0)
            Debug.LogError(Tag + "reached no first-party node instance, so it proves nothing: the node walk is broken, not the bindings. "
                + "Do not read the empty list above as clean.");
        else if (nulls.Count == 0) Debug.Log(line + " -> OK, every binding arrived.");
        else Debug.LogWarning(line + " -> " + nulls.Count + " distinct null binding(s), listed above.");
    }

    /// <summary>
    /// Depth-limited reflection walk collecting every node instance reachable from an object.
    /// It stops at UnityEngine.Object values so it never wanders into assets or the scene.
    /// </summary>
    static void CollectNodes(object from, Type nodeType, HashSet<object> visited, List<object> found, int depth)
    {
        if (from == null || depth > 10 || from is string) return;
        // The root IS a ScriptableObject (the graph instance), so only nested UnityEngine.Object
        // values are off limits: they lead to assets and the scene, never to nodes.
        if (depth > 0 && from is UnityEngine.Object) return;
        if (!visited.Add(from)) return;

        if (nodeType != null && nodeType.IsInstanceOfType(from)) found.Add(from);

        var level = from.GetType();
        while (level != null && level != typeof(object))
        {
            var fields = level.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            for (int i = 0; i < fields.Length; i++)
            {
                var fieldType = fields[i].FieldType;
                if (fieldType.IsValueType || fieldType == typeof(string) || typeof(Delegate).IsAssignableFrom(fieldType)) continue;
                object value;
                try { value = fields[i].GetValue(from); }
                catch (Exception) { continue; }
                if (value == null || value is UnityEngine.Object) continue;

                var list = value as System.Collections.IEnumerable;
                if (list != null)
                {
                    foreach (var item in list) CollectNodes(item, nodeType, visited, found, depth + 1);
                    continue;
                }
                CollectNodes(value, nodeType, visited, found, depth + 1);
            }
            level = level.BaseType;
        }
    }

    /// <summary>Which enemy or NPC a node belongs to, so a live finding is recognisable.</summary>
    static string DescribeOwner(object node)
    {
        try
        {
            var property = node.GetType().GetProperty("GameObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null) return "owner unknown";
            var owner = property.GetValue(node, null) as GameObject;
            return owner == null ? "owner not in a scene" : "on " + owner.name;
        }
        catch (Exception)
        {
            return "owner unknown";
        }
    }

    // ------------------------------------------------------------------ validator self-test
    /// <summary>
    /// Proves the checker itself works, which is the only thing that makes a green run worth
    /// anything. Every case is the exact shape commit 574a7847 (T-026) created: the field
    /// EnemyGroupContoller became GroupController, the type EnemiesGroupContoller became
    /// EnemiesGroupController, and the graphs kept the old field key. If a rename of that shape ever
    /// stops being caught here, Validate() above stops being trustworthy - so Validate() runs this
    /// first and refuses to report a clean result when it fails.
    /// </summary>
    [MenuItem("Tools/Ciro/Validate Graph Node Bindings (Validator Self-Test)")]
    public static void SelfTestMenuItem()
    {
        var failures = RunSelfTestCollect();
        if (failures.Count == 0)
            Debug.Log(Tag + "self-test OK: a renamed node field or a renamed BlackboardVariable<T> would be caught. "
                + "The T-026 shape (EnemyGroupContoller -> GroupController) is one of the cases.");
        else
            for (int i = 0; i < failures.Count; i++) Debug.LogError(Tag + "self-test: " + failures[i]);
    }

    static int RunSelfTest()
    {
        var failures = RunSelfTestCollect();
        for (int i = 0; i < failures.Count; i++) Debug.LogError(Tag + "self-test: " + failures[i]);
        return failures.Count;
    }

    static List<string> RunSelfTestCollect()
    {
        var failures = new List<string>();

        var node = Resolve("AlertGroupAction", FirstPartyAsm);
        if (node == null)
        {
            failures.Add("AlertGroupAction does not resolve in " + FirstPartyAsm + ", so the rest of the self-test cannot judge anything.");
            return failures;
        }

        const string brokenName = "Unity.Behavior.BlackboardVariable`1[[EnemiesGroupContoller, Assembly-CSharp]]";
        const string currentName = "Unity.Behavior.BlackboardVariable`1[[EnemiesGroupController, Assembly-CSharp]]";

        // The name the graphs carried BEFORE the sweep must be unreadable now, and check 3 must be
        // able to name the type that disappeared.
        if (Resolve(currentName, "Unity.Behavior") == null)
            failures.Add("the current closed name BlackboardVariable<EnemiesGroupController> should resolve but did not: check 3 would cry wolf.");
        if (Resolve(brokenName, "Unity.Behavior") != null)
            failures.Add("the pre-rename name EnemiesGroupContoller still resolves, so a renamed type could slip through check 3.");
        var missing = MissingArgument(brokenName);
        if (missing != "EnemiesGroupContoller")
            failures.Add("MissingArgument named " + Q(missing) + " instead of 'EnemiesGroupContoller'.");

        // The key the graphs still write, versus the field the class now has: checks 1 and 2.
        var fields = FieldNames(node, null);
        if (!fields.Contains("GroupController")) failures.Add("FieldNames is missing the current field 'GroupController'.");
        if (!fields.Contains("Self")) failures.Add("FieldNames is missing the current field 'Self'.");
        if (!fields.Contains("m_Parent")) failures.Add("FieldNames is missing the inherited 'm_Parent' (Unity.Behavior.Action): every node would look stale.");
        if (!fields.Contains("ID") || !fields.Contains("Graph")) failures.Add("FieldNames is missing the inherited 'ID'/'Graph' (Unity.Behavior.Node).");
        if (fields.Contains("EnemyGroupContoller")) failures.Add("FieldNames reports 'EnemyGroupContoller' as a field, so the stale key in the 6 enemy graphs would never be flagged.");

        var bindings = BindingFields(node);
        if (bindings.Count != 2) failures.Add("BindingFields found " + bindings.Count + " BlackboardVariable bindings on AlertGroupAction, expected 2 (GroupController, Self).");
        if (!IsNodeType(node)) failures.Add("IsNodeType does not recognise AlertGroupAction, so check 2 would skip every project node.");

        var lookalike = ClosestField(node, "GroupControllerOld");
        if (lookalike != "GroupController") failures.Add("ClosestField named " + Q(lookalike) + " instead of 'GroupController'.");

        // The reader, on the exact formats Unity writes.
        var sample = new Record();
        ParseTypeLine("      type: {class: 'BlackboardVariable`1[[System.String, mscorlib]]', ns: Unity.Behavior, asm: Unity.Behavior}", sample);
        if (sample.ClassName != "BlackboardVariable`1[[System.String, mscorlib]]")
            failures.Add("ParseTypeLine read the class as " + Q(sample.ClassName) + ".");
        if (sample.Namespace != "Unity.Behavior" || sample.Assembly != "Unity.Behavior")
            failures.Add("ParseTypeLine read ns/asm as " + Q(sample.Namespace) + "/" + Q(sample.Assembly) + ".");

        string fullName, assemblyName;
        if (!SplitSerializableType("AlertGroupAction, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
                out fullName, out assemblyName) || fullName != "AlertGroupAction" || assemblyName != FirstPartyAsm)
            failures.Add("SplitSerializableType read " + Q(fullName) + "/" + Q(assemblyName) + " out of an assembly-qualified type string.");

        // End to end on a real graph: the reader has to find records, node models and field keys.
        var graphs = FindAssets("t:BehaviorAuthoringGraph");
        if (graphs.Count > 0)
        {
            List<Record> records;
            List<NodeDef> defs;
            int[] flags;
            ParseAsset(graphs[0], out records, out defs, out flags);
            if (records.Count < 50) failures.Add(graphs[0] + " parsed to " + records.Count + " managed reference records - the reader lost the format.");
            if (defs.Count < 3) failures.Add(graphs[0] + " parsed to " + defs.Count + " node models - the m_NodeModelsInfo reader is broken.");

            int firstPartyNodes = 0;
            var typeCache = new Dictionary<string, Type>(StringComparer.Ordinal);
            for (int i = 0; i < records.Count; i++)
            {
                if (!records[i].HasType || records[i].Keys.Count == 0) continue;
                if (!string.Equals(records[i].Assembly, FirstPartyAsm, StringComparison.Ordinal)) continue;
                var type = ResolveCached(records[i].FullType, records[i].Assembly, typeCache);
                if (type != null && IsNodeType(type)) firstPartyNodes++;
            }
            if (firstPartyNodes == 0) failures.Add(graphs[0] + " has no first-party node record with a field key: checks 1 and 2 would have nothing to read.");
        }

        return failures;
    }
}




