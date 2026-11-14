using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// T-045 Phase 3 (step G): removes the camera-based minimap that the art map replaced.
///
/// Three separate menu items on purpose, in this order, because the standing lesson from
/// Phase 2 is that batching every scene into one menu run is what wedged the editor:
///
///  1. Report  - reads the .unity files as text and opens nothing. Safe to run any time,
///     including after the assets are gone.
///  2. Remove  - works on the ONE scene that is currently open, asks before it deletes,
///     and leaves the save to you. Run it once per scene, heaviest (Forest) last.
///  3. Assets  - deletes the old folder, prefab and material, and refuses to run while any
///     placement still points at them.
///
/// Placements are matched by the legacy prefab's GUID, never by name: HUD.prefab has a
/// GameObject called "Minimap" that is the NEW map's viewport (MapView.minimapViewport) and
/// must survive this. Nothing here references MinimapCamera / MinimapIcon / MinimapMaterial
/// as types, so the tool still compiles after step 3 removes them.
/// </summary>
internal static class MapLegacyMinimapPurge
{
    private const string LegacyPrefabPath   = "Assets/Prefabs/HUD/MinimapCamera.prefab";
    private const string LegacyFolder       = "Assets/MiniMap";
    private const string LegacyMaterialPath = "Assets/Materials/MinimapMaterial.mat";
    private const string ScenesFolder       = "Assets/Scenes/";
    private const string Tag                = "[MapPhase3]";

    // Kept out of every scan: the crash snapshots are T-030's business, not ours.
    private const string Excluded = "_Recovery";

    [MenuItem("Tools/Map/Phase 3 - Report Legacy Minimap (read only)")]
    public static void Report()
    {
        string guid = LegacyGuid();
        Debug.Log(Tag + " report - nothing was opened and nothing was modified.");
        Debug.Log(Tag + " legacy prefab " + LegacyPrefabPath + " -> GUID " + (guid ?? "<asset is already gone>"));

        Inventory();

        var hits = ScanSceneFiles(guid, false);
        if (hits.Count == 0)
            Debug.Log(Tag + " no scene places the legacy camera anywhere. Step 2 is done.");
        else
            Debug.Log(Tag + " " + hits.Count + " scene(s) still place it - see the list above. Open one and run "
                      + "'Phase 3 - Remove From OPEN Scene' on it.");
    }

    /// <summary>Counts what each of the doomed assets is still referenced by, outside itself.</summary>
    private static void Inventory()
    {
        var assets = new List<KeyValuePair<string, string>>();
        AddAsset(assets, "MinimapCamera.cs",       LegacyFolder + "/MinimapCamera.cs");
        AddAsset(assets, "MinimapIcon.cs",         LegacyFolder + "/MinimapIcon.cs");
        AddAsset(assets, "MinimapMaterial.cs",     LegacyFolder + "/MinimapMaterial.cs");
        AddAsset(assets, "MinimapShape.renderTex", LegacyFolder + "/MinimapShape.renderTexture");
        AddAsset(assets, "MinimapCamera.prefab",   LegacyPrefabPath);
        AddAsset(assets, "MinimapMaterial.mat",    LegacyMaterialPath);

        var files = ScanTargets();
        for (int i = 0; i < assets.Count; i++)
        {
            string label = assets[i].Key;
            string guid  = assets[i].Value;
            if (string.IsNullOrEmpty(guid))
            {
                Debug.Log(Tag + " " + label + ": not present - already deleted.");
                continue;
            }

            int total = 0;
            var where = new List<string>();
            for (int f = 0; f < files.Count; f++)
            {
                int n = CountIn(files[f], guid);
                if (n == 0) continue;
                total += n;
                where.Add(Path.GetFileName(files[f]) + " x" + n);
            }
            Debug.Log(Tag + " " + label + ": " + total + " reference(s) - " + string.Join(", ", where));
        }
    }

    private static void AddAsset(List<KeyValuePair<string, string>> into, string label, string path)
    {
        if (File.Exists(path) == false) { into.Add(new KeyValuePair<string, string>(label, null)); return; }
        into.Add(new KeyValuePair<string, string>(label, AssetDatabase.AssetPathToGUID(path)));
    }

    /// <summary>Every scene / prefab / asset that could point at the old camera, minus the crash snapshots.</summary>
    private static List<string> ScanTargets()
    {
        var list = new List<string>();
        var found = Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories);
        for (int i = 0; i < found.Length; i++)
        {
            string path = found[i].Replace('\\', '/');
            if (path.Contains("/" + Excluded + "/")) continue;
            if (path.EndsWith(".meta")) continue;
            string ext = Path.GetExtension(path);
            if (ext == ".unity" || ext == ".prefab" || ext == ".asset" || ext == ".controller" || ext == ".cs")
                list.Add(path);
        }
        return list;
    }

    private static int CountIn(string path, string needle)
    {
        string text;
        try { text = File.ReadAllText(path); }
        catch { return 0; }
        return Regex.Matches(text, Regex.Escape(needle)).Count;
    }

    private static string LegacyGuid()
    {
        if (File.Exists(LegacyPrefabPath) == false) return null;
        string guid = AssetDatabase.AssetPathToGUID(LegacyPrefabPath);
        return string.IsNullOrEmpty(guid) ? null : guid;
    }

    /// <summary>
    /// Finds every scene that places the legacy camera, from the YAML text, without opening a scene.
    /// Returns the scene paths; logs the per-scene detail unless quiet.
    /// </summary>
    private static List<string> ScanSceneFiles(string guid, bool quiet)
    {
        var found = new List<string>();
        if (string.IsNullOrEmpty(guid)) return found;

        var files = ScanTargets();
        for (int i = 0; i < files.Count; i++)
        {
            if (files[i].EndsWith(".unity") == false) continue;
            string text;
            try { text = File.ReadAllText(files[i]); }
            catch { continue; }
            if (text.Contains(guid) == false) continue;

            found.Add(files[i]);
            if (quiet) continue;

            // The PrefabInstance anchors, so the numbers can be compared with the audit tables
            // in the task file. The reference COUNT is not the number of placements: overrides
            // repeat the GUID, which is the measurement bug the reference note warns about.
            var lines = text.Replace("\r\n", "\n").Split('\n');
            var anchors = new List<string>();
            for (int l = 0; l < lines.Length; l++)
            {
                if (lines[l].Contains(guid) == false) continue;
                for (int b = l; b >= 0 && b > l - 400; b--)
                {
                    var m = Regex.Match(lines[b], @"^--- !u!1001 &(\d+)");
                    if (m.Success) { if (anchors.Contains(m.Groups[1].Value) == false) anchors.Add(m.Groups[1].Value); break; }
                }
            }

            string extra = "";
            var open = EditorSceneManager.GetActiveScene();
            if (open.IsValid() && open.path == files[i])
                extra = open.isDirty ? "  <-- OPEN here and dirty" : "  <-- OPEN here";
            for (int s = 0; s < EditorBuildSettings.scenes.Length; s++)
            {
                if (EditorBuildSettings.scenes[s].path != files[i]) continue;
                extra += EditorBuildSettings.scenes[s].enabled ? "  (in build)" : "  (in build, disabled)";
            }

            Debug.Log(Tag + " " + files[i] + ": anchors " + string.Join(",", anchors) + extra);
        }
        return found;
    }

    // ---------------------------------------------------------------- step 2

    [MenuItem("Tools/Map/Phase 3 - Remove From OPEN Scene")]
    public static void RemoveFromOpenScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.IsValid() == false || scene.isLoaded == false)
        {
            Debug.LogError(Tag + " no scene is open. Open ONE scene and run this again - it never batch-loads.");
            return;
        }

        // Additive loading is what made Phase 2 resolve objects from the WRONG scene, so say plainly
        // which scene this pass is allowed to touch.
        if (EditorSceneManager.sceneCount > 1)
        {
            var others = new List<string>();
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var other = EditorSceneManager.GetSceneAt(i);
                if (other.IsValid() && other.path != scene.path) others.Add(other.name);
            }
            if (others.Count > 0)
                Debug.LogWarning(Tag + " " + others.Count + " extra scene(s) are loaded additively ("
                               + string.Join(", ", others) + "). This tool touches the ACTIVE scene only ("
                               + scene.name + ") - open the others one at a time and run it again.");
        }

        string guid = LegacyGuid();
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogError(Tag + " " + LegacyPrefabPath + " is already gone, so a leftover placement cannot be "
                         + "matched by GUID. Find it by name in the Hierarchy and delete it by hand.");
            return;
        }

        var roots = scene.GetRootGameObjects();
        var doomed = new List<GameObject>();
        for (int i = 0; i < roots.Length; i++)
        {
            var all = roots[i].GetComponentsInChildren<Transform>(true);
            for (int t = 0; t < all.Length; t++)
            {
                var go = all[t].gameObject;
                if (PrefabUtility.IsPartOfPrefabInstance(go) == false) continue;
                var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
                if (src == null) continue;
                if (AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(src)) != guid) continue;

                if (t > 0)
                {
                    Debug.LogWarning(Tag + " " + go.name + " is nested under " + GetPath(all[t])
                                   + ", but the audit says every placement is a scene root. Skipping it - check by hand.");
                    continue;
                }
                doomed.Add(go);
            }
        }

        if (doomed.Count == 0)
        {
            Debug.Log(Tag + " " + scene.name + " places no legacy camera. Nothing to do.");
            return;
        }

        var names = new List<string>();
        for (int i = 0; i < doomed.Count; i++)
            names.Add(doomed[i].name + " (children: " + doomed[i].transform.childCount + ")");

        if (scene.isDirty)
            Debug.LogWarning(Tag + " " + scene.name + " was already dirty before this tool touched it; saving "
                           + "writes those edits too.");

        string message = "About to delete from " + scene.path + ":\n  - " + string.Join("\n  - ", names)
                       + "\n\nOnly objects instantiated from " + LegacyPrefabPath + " are in that list."
                       + "\nThe node named Minimap inside HUD.prefab is a different object and is not touched.";
        if (EditorUtility.DisplayDialog("Phase 3 - remove the legacy minimap camera", message, "Delete and save", "Cancel") == false)
        {
            Debug.Log(Tag + " cancelled. " + scene.name + " was not modified.");
            return;
        }

        for (int i = 0; i < doomed.Count; i++)
        {
            Debug.Log(Tag + " deleted " + doomed[i].name + " (" + GetPath(doomed[i].transform) + ") from " + scene.name);
            Object.DestroyImmediate(doomed[i]);
        }

        if (EditorSceneManager.SaveScene(scene) == false)
            Debug.LogError(Tag + " could not save " + scene.path + ". Save it yourself before opening another scene.");
        else
            Debug.Log(Tag + " saved " + scene.path + ". Next scene, please - and Forest last, it is the heaviest.");
    }


    // ---------------------------------------------------------------- step 3

    [MenuItem("Tools/Map/Phase 3 - Delete Legacy Assets (needs 0 placements)")]
    public static void DeleteLegacyAssets()
    {
        // Gate: no scene may still point at the prefab. Refuse rather than leave missing references.
        var stillPlaced = ScanSceneFiles(LegacyGuid(), true);
        if (stillPlaced.Count > 0)
        {
            Debug.LogError(Tag + " refusing to delete anything: " + stillPlaced.Count
                         + " scene(s) still place " + LegacyPrefabPath + " (" + string.Join(", ", stillPlaced)
                         + "). Open each one and run 'Phase 3 - Remove From OPEN Scene' first.");
            return;
        }

        var targets = new List<string>();
        if (Directory.Exists(LegacyFolder))
        {
            var inside = Directory.GetFiles(LegacyFolder, "*.*", SearchOption.AllDirectories);
            var names = new List<string>();
            for (int i = 0; i < inside.Length; i++)
                if (inside[i].EndsWith(".meta") == false) names.Add(Path.GetFileName(inside[i]));
            Debug.Log(Tag + " " + LegacyFolder + " holds " + string.Join(", ", names));
            targets.Add(LegacyFolder);
        }
        if (File.Exists(LegacyPrefabPath))   targets.Add(LegacyPrefabPath);
        if (File.Exists(LegacyMaterialPath)) targets.Add(LegacyMaterialPath);

        if (targets.Count == 0)
        {
            Debug.Log(Tag + " nothing left to delete - Phase 3 step 3 is already done.");
            return;
        }

        string message = "Delete these, plus their .meta files?\n  - " + string.Join("\n  - ", targets)
                       + "\n\nNo scene places the camera anymore. The render texture it rendered is sampled by nothing."
                       + "\nThis is a normal asset deletion: undo is NOT available, git is.";
        if (EditorUtility.DisplayDialog("Phase 3 - delete the legacy minimap assets", message, "Delete", "Cancel") == false)
        {
            Debug.Log(Tag + " cancelled. Nothing was deleted.");
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            bool ok = AssetDatabase.DeleteAsset(targets[i]);
            if (ok) Debug.Log(Tag + " deleted " + targets[i]);
            else Debug.LogError(Tag + " could NOT delete " + targets[i] + " - check the Editor is not holding it.");
        }

        AssetDatabase.Refresh();
        Debug.Log(Tag + " done. Now, in this order: force a refresh and read the Console (0 errors expected),"
                + " then Tools/Map/Validate All Definitions, then play Village and Forest - map opens, markers"
                + " draw, the camera still holds, 0 Console errors. Then re-count Assets/Scripts for the Script Index.");
    }

    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}

