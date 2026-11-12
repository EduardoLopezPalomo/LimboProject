using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires the five built scenes for the T-045 map system: adds MapView, MapSceneBinder and
/// MapService to the scene's existing MapToggle host, points them at that scene's MapDefinition,
/// the shared fallback and the icon catalog, finishes the MapToggle wiring, and deletes the empty
/// duplicate MapManager that T-043 tracks.
///
/// Scene-explicit on purpose. Migration 2 opens FirstForest additively and leaves it loaded when it
/// changed something, so a FindFirstObjectByType during a later pass can return an object from the
/// WRONG scene (measured 11/11/2026: with both Forest and FirstForest loaded, three MapToggles are
/// live and two of them are named MapManager). Every lookup here goes through a root list of one
/// specific scene, and each scene is opened in Single mode first.
///
/// Fails loud and refuses to half-wire: a scene either gets everything and is saved, or nothing is
/// assigned to it and the console says exactly which object was missing.
/// </summary>
internal static class MapSceneWiring
{
    private const string MapFolder = "Assets/UI Images/Map/";
    private const string DefinitionsFolder = MapFolder + "Definitions/";
    private const string FallbackAsset = MapFolder + "Map_Fallback.asset";
    private const string CatalogAsset = MapFolder + "MapIconCatalog.asset";
    private const string ScenesFolder = "Assets/Scenes/";

    private const string MinimapViewportPath = "UI/HUD/EyeUI_Small/Minimap";
    private const string BigMapViewportPath = "UI/HUD/EyeUI_BIG/Canvas";

    private static readonly string[] BuiltScenes = { "FirstForest", "Forest", "Village", "Party", "SpaceBoss" };

    [MenuItem("Tools/Map/Wire Built Scenes (report only)")]
    public static void Report()
    {
        for (int i = 0; i < BuiltScenes.Length; i++)
            Inspect(BuiltScenes[i], false);

        Debug.Log("[MapSceneWiring] Report done. Nothing was modified. Run 'Tools/Map/Wire Built Scenes' to apply.");
    }

    [MenuItem("Tools/Map/Wire Built Scenes")]
    public static void WireAll()
    {
        var failures = new List<string>();
        for (int i = 0; i < BuiltScenes.Length; i++)
        {
            if (!Inspect(BuiltScenes[i], true))
                failures.Add(BuiltScenes[i]);
        }

        if (failures.Count == 0)
            Debug.Log("[MapSceneWiring] All " + BuiltScenes.Length + " scenes wired and saved.");
        else
            Debug.LogError("[MapSceneWiring] NOT wired (nothing half-applied): " + string.Join(", ", failures)
                         + ". Fix the missing objects named in the log above and run it again - it is idempotent.");
    }

    /// <summary>Returns true when the scene is complete (wired, or already wired and still consistent).</summary>
    private static bool Inspect(string sceneName, bool apply)
    {
        string scenePath = ScenesFolder + sceneName + ".unity";
        if (File.Exists(scenePath) == false)
        {
            Debug.LogError("[MapSceneWiring] " + scenePath + " does not exist.");
            return false;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (scene.IsValid() == false || scene.isLoaded == false)
        {
            Debug.LogError("[MapSceneWiring] Could not open " + scenePath + ".");
            return false;
        }

        var report = new List<string>();
        var missing = new List<string>();

        // --- the driver: exactly one MapToggle with miniRoot assigned -----------------------------
        var driver = FindDriver(sceneName, scene.GetRootGameObjects(), missing);
        if (driver == null)
            return Bail(sceneName, missing);

        // --- the viewports and the player ---------------------------------------------------------
        RectTransform miniViewport = FindRect(scene, MinimapViewportPath, missing);
        RectTransform bigViewport = FindRect(scene, BigMapViewportPath, missing);

        GameObject playerGo = null;
        StarterAssets.StarterAssetsInputs inputs = null;
        var playerTag = scene.IsValid() ? FindPlayerWithTag(scene.GetRootGameObjects()) : null;
        if (playerTag == null)
        {
            missing.Add("a GameObject tagged Player");
        }
        else
        {
            playerGo = playerTag;
            inputs = playerTag.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (inputs == null)
                missing.Add(playerTag.name + " (the Player) has no StarterAssetsInputs component");
        }

        // --- the data assets ----------------------------------------------------------------------
        string defPath = DefinitionsFolder + "Map_" + sceneName + ".asset";
        var definition = AssetDatabase.LoadAssetAtPath<MapDefinition>(defPath);
        if (definition == null)
            missing.Add(defPath);

        var fallback = AssetDatabase.LoadAssetAtPath<MapDefinition>(FallbackAsset);
        if (fallback == null)
            missing.Add(FallbackAsset);

        var catalog = AssetDatabase.LoadAssetAtPath<MapIconCatalog>(CatalogAsset);
        if (catalog == null)
            missing.Add(CatalogAsset);

        if (missing.Count > 0)
            return Bail(sceneName, missing);

        // --- components on the driver host ---------------------------------------------------------
        var host = driver.gameObject;
        var view = GetOrAdd<MapView>(host, report);
        var binder = GetOrAdd<MapSceneBinder>(host, report);
        var service = GetOrAdd<MapService>(host, report);

        if (apply)
        {
            Assign(view, "minimapViewport", miniViewport, report);
            Assign(view, "bigMapViewpport", bigViewport, report);
            Assign(binder, "definition", definition, report);
            Assign(binder, "fallback", fallback, report);
            Assign(binder, "iconCatalog", catalog, report);
            Assign(service, "view", view, report);
            Assign(service, "binder", binder, report);
            Assign(service, "player", playerGo.transform, report);
            Assign(driver, "mapService", service, report);
            Assign(driver, "playerInputs", inputs, report);
        }

        // --- T-043: the empty duplicate MapManager, if this scene has one --------------------------
        int duplicatesRemoved = apply ? RemoveEmptyMapManagers(sceneName, scene.GetRootGameObjects(), report) : CountEmptyMapManagers(scene.GetRootGameObjects());

        Debug.Log("[MapSceneWiring] " + sceneName + (apply ? " APPLIED" : " REPORT")
                + " | host=" + host.name + " | definition=" + Path.GetFileName(defPath)
                + " | miniViewport=" + MinimapViewportPath + " | bigViewport=" + BigMapViewportPath
                + " | empty duplicates " + (apply ? "removed" : "found") + "=" + duplicatesRemoved
                + "\n    " + string.Join("\n    ", report));

        if (apply)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene) == false)
            {
                Debug.LogError("[MapSceneWiring] Could not save " + scenePath + " - the wiring is in the editor only.");
                return false;
            }
        }

        return true;
    }

    private static bool Bail(string sceneName, List<string> missing)
    {
        Debug.LogError("[MapSceneWiring] " + sceneName + " NOT wired, nothing was assigned. Missing:\n  - "
                     + string.Join("\n  - ", missing));
        return false;
    }

    /// <summary>
    /// The one MapToggle that drives the map: miniRoot assigned. An all-null one is the T-043
    /// duplicate and is never the driver. More than one wired driver in a scene is fatal, because
    /// every one of them subscribes to the same OpenMap event and they would fight over the same
    /// eye animation.
    /// </summary>
    private static MapToggle FindDriver(string sceneName, GameObject[] roots, List<string> missing)
    {
        var wired = new List<MapToggle>();
        foreach (var root in roots)
        {
            var found = root.GetComponentsInChildren<MapToggle>(true);
            for (int i = 0; i < found.Length; i++)
            {
                var so = new SerializedObject(found[i]);
                var miniRoot = so.FindProperty("miniRoot");
                if (miniRoot != null && miniRoot.objectReferenceValue != null) wired.Add(found[i]);
            }
        }

        if (wired.Count == 0)
        {
            missing.Add("a MapToggle with miniRoot assigned (every MapManager in this scene is empty)");
            return null;
        }

        if (wired.Count > 1)
        {
            var paths = new List<string>();
            for (int i = 0; i < wired.Count; i++) paths.Add(GetPath(wired[i].transform));
            missing.Add("exactly one wired MapToggle but found " + wired.Count + ": " + string.Join(" | ", paths)
                      + " - two drivers on one OpenMap event would fight, decide which to keep by hand");
            return null;
        }

        return wired[0];
    }

    /// <summary>
    /// Resolves a path that may or may not start with the scene root's own name. The paths in this
    /// tool are written the way the probe prints them (root-inclusive, e.g. "UI/HUD/EyeUI_Small/
    /// Minimap"), but Transform.Find is relative to the transform it is called on, so a root called
    /// "UI" never matches a path whose first segment is "UI" - which is why the first version of this
    /// tool reported every viewport missing. Both spellings are accepted.
    /// </summary>
    private static RectTransform FindRect(Scene scene, string relativePath, List<string> missing)
    {
        string[] segments = relativePath.Split('/');
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform root = roots[i].transform;
            int start = root.name == segments[0] ? 1 : 0;
            Transform found = WalkDown(root, segments, start);
            if (found == null) continue;

            var rect = found as RectTransform;
            if (rect == null)
            {
                missing.Add(root.name + "/" + relativePath + " exists but is not a RectTransform");
                return null;
            }
            return rect;
        }

        missing.Add("viewport " + relativePath + " (no root in this scene contains that path)");
        return null;
    }

    private static Transform WalkDown(Transform from, string[] segments, int startIndex)
    {
        for (int i = startIndex; i < segments.Length; i++)
        {
            Transform next = from.Find(segments[i]);
            if (next == null) return null;
            from = next;
        }
        return from;
    }

    private static GameObject FindPlayerWithTag(GameObject[] roots)
    {
        for (int i = 0; i < roots.Length; i++)
        {
            var all = roots[i].GetComponentsInChildren<Transform>(true);
            for (int c = 0; c < all.Length; c++)
                if (all[c].CompareTag("Player")) return all[c].gameObject;
        }
        return null;
    }

    private static T GetOrAdd<T>(GameObject host, List<string> report) where T : Component
    {
        var existing = host.GetComponent<T>();
        if (existing != null)
        {
            report.Add(typeof(T).Name + ": already present");
            return existing;
        }

        report.Add(typeof(T).Name + ": ADDED to " + host.name);
        return host.AddComponent<T>();
    }

    private static void Assign(Component target, string field, Object value, List<string> report)
    {
        var so = new SerializedObject(target);
        var property = so.FindProperty(field);
        if (property == null)
        {
            Debug.LogError("[MapSceneWiring] " + target.GetType().Name + " has no serialized field '" + field
                         + "' - the tool is out of date with the component.");
            return;
        }

        Object before = property.objectReferenceValue;
        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();

        var check = new SerializedObject(target);
        Object written = check.FindProperty(field).objectReferenceValue;
        if (written != value)
        {
            Debug.LogError("[MapSceneWiring] " + target.name + "." + field + " was asked for "
                         + (value == null ? "null" : value.name) + " but holds "
                         + (written == null ? "null" : written.name) + ".");
            return;
        }

        if (before != value)
            report.Add(target.GetType().Name + "." + field + " = " + (value == null ? "null" : value.name));
    }

    private static int CountEmptyMapManagers(GameObject[] roots)
    {
        int count = 0;
        foreach (var root in roots)
        {
            var found = root.GetComponentsInChildren<MapToggle>(true);
            for (int i = 0; i < found.Length; i++)
                if (IsEmpty(found[i])) count++;
        }
        return count;
    }

    private static int RemoveEmptyMapManagers(string sceneName, GameObject[] roots, List<string> report)
    {
        var empties = new List<MapToggle>();
        foreach (var root in roots)
        {
            var found = root.GetComponentsInChildren<MapToggle>(true);
            for (int i = 0; i < found.Length; i++)
                if (IsEmpty(found[i])) empties.Add(found[i]);
        }

        for (int i = 0; i < empties.Count; i++)
        {
            var go = empties[i].gameObject;
            if (go.transform.childCount > 0)
            {
                Debug.LogWarning("[MapSceneWiring] " + sceneName + ": the empty MapManager at " + GetPath(go.transform)
                               + " has children, so it was disabled instead of deleted. Check by hand.");
                go.SetActive(false);
                continue;
            }

            report.Add("T-043: deleted empty " + go.name + " at " + GetPath(go.transform));
            Object.DestroyImmediate(go);
        }

        return empties.Count;
    }

    private static bool IsEmpty(MapToggle toggle)
    {
        var so = new SerializedObject(toggle);
        string[] fields = { "miniRoot", "bigRoot", "miniEye", "bigEye" };
        for (int i = 0; i < fields.Length; i++)
        {
            var p = so.FindProperty(fields[i]);
            if (p != null && p.objectReferenceValue != null) return false;
        }
        return true;
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
