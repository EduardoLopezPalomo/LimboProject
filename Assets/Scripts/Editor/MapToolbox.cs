using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot authoring helpers for the T-045 map system. Everything here writes assets and logs what
/// it wrote; none of it runs at play time.
///
/// Menus:
/// Tools/Map/Seed Icon Catalog
/// Tools/Map/Create Placeholder Art And Fallback
/// Tools/Map/Create Definitions For Built Scenes
/// Tools/Map/Raise Base Maps To 4096
/// Tools/Map/Validate All Definitions
/// </summary>
internal static class MapToolbox
{
    private const string MapRoot = "Assets/UI Images/Map";
    private const string BaseFolder = MapRoot + "/Base";
    private const string ElementsFolder = MapRoot + "/Elements";
    private const string CatalogPath = MapRoot + "/MapIconCatalog.asset";
    private const string FallbackPath = MapRoot + "/Map_Fallback.asset";
    private const string PlaceholderPath = BaseFolder + "/map_placeholder.png";

    /// <summary>MapIconType to the file-name stem in Elements/. Every icon is "&lt;slug&gt;_icon.png"
    /// except mission_point, so the lookup tries both.</summary>
    private static readonly MapIconType[] IconOrder =
    {
        MapIconType.Enemy, MapIconType.MonsterNpc, MapIconType.HumanNpc, MapIconType.MainCharacter,
        MapIconType.MissionPoint, MapIconType.Gods, MapIconType.Teleport, MapIconType.BossCave,
        MapIconType.Camp, MapIconType.Fire, MapIconType.DeadTree, MapIconType.TreeStump,
        MapIconType.Cage, MapIconType.Boxes, MapIconType.FishMarket, MapIconType.ForgeMarket,
        MapIconType.LuxuryMarket, MapIconType.MagicMarket, MapIconType.MeatMarket, MapIconType.VegetableMarket
    };

    private static readonly Dictionary<MapIconType, string> Slugs = new Dictionary<MapIconType, string>
    {
        { MapIconType.Enemy, "enemy" },
        { MapIconType.MonsterNpc, "monsters_npc" },
        { MapIconType.HumanNpc, "human_npc" },
        { MapIconType.MainCharacter, "main_character" },
        { MapIconType.MissionPoint, "mission_point" },
        { MapIconType.Gods, "gods" },
        { MapIconType.Teleport, "teleport" },
        { MapIconType.BossCave, "boss_cave" },
        { MapIconType.Camp, "camp" },
        { MapIconType.Fire, "fire" },
        { MapIconType.DeadTree, "dead_tree" },
        { MapIconType.TreeStump, "tree_stump" },
        { MapIconType.Cage, "cage" },
        { MapIconType.Boxes, "boxes" },
        { MapIconType.FishMarket, "fish_market" },
        { MapIconType.ForgeMarket, "forge_market" },
        { MapIconType.LuxuryMarket, "luxury_market" },
        { MapIconType.MagicMarket, "magic_market" },
        { MapIconType.MeatMarket, "meat_market" },
        { MapIconType.VegetableMarket, "vegetable_market" }
    };

    /// <summary>The five built scenes and the drawing each one belongs to (D-11 Q5, confirmed 1:1).</summary>
    private static readonly string[,] SceneToDrawing =
    {
        { "FirstForest", "first_forest_map" },
        { "Forest", "forest_map_v2" },
        { "Village", "town_map_part2 1" },
        { "Party", "party_map 1" },
        { "SpaceBoss", "space-time_map" }
    };

    [MenuItem("Tools/Map/Seed Icon Catalog")]
    public static void SeedIconCatalog()
    {
        MapIconCatalog catalog = LoadOrCreateCatalog();

        int filled = 0;
        int missing = 0;
        for (int i = 0; i < IconOrder.Length; i++)
        {
            MapIconType type = IconOrder[i];
            Sprite sprite = FindIcon(type);
            if (sprite == null)
            {
                missing++;
                Debug.LogError($"[MapToolbox] No drawing in '{ElementsFolder}' for {type} (looked for '{Slugs[type]}_icon' and '{Slugs[type]}').");
                continue;
            }

            catalog.SetEntry(type, sprite);
            filled++;
        }

        Sprite fallback = FindIcon(MapIconType.MissionPoint);
        if (fallback != null) catalog.SetMissingSprite(fallback);

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log($"[MapToolbox] Icon catalog seeded at {CatalogPath}: {filled} of {IconOrder.Length} types mapped, {missing} missing. Fallback drawing: {(fallback == null ? "NONE - set one in the Inspector" : fallback.name)}");
    }

    private static MapIconCatalog LoadOrCreateCatalog()
    {
        MapIconCatalog catalog = AssetDatabase.LoadAssetAtPath<MapIconCatalog>(CatalogPath);
        if (catalog != null) return catalog;

        catalog = ScriptableObject.CreateInstance<MapIconCatalog>();
        AssetDatabase.CreateAsset(catalog, CatalogPath);
        Debug.Log($"[MapToolbox] Created {CatalogPath}");
        return catalog;
    }

    private static Sprite FindIcon(MapIconType type)
    {
        string slug = Slugs[type];
        Sprite sprite = LoadSprite(ElementsFolder + "/" + slug + "_icon.png");
        if (sprite != null) return sprite;

        return LoadSprite(ElementsFolder + "/" + slug + ".png");
    }

    private static Sprite LoadSprite(string path)
    {
        if (File.Exists(path) == false) return null;

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;

        Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < all.Length; i++)
            if (all[i] is Sprite found) return found;

        return null;
    }

    /// <summary>
    /// The placeholder drawing for scenes that have no art yet (D-11 Q5). Procedural rather than
    /// waiting on art, because it is one small asset and it is what makes the key work everywhere
    /// instead of silently dying. Art can drop a real drawing over this path later.
    /// </summary>
    [MenuItem("Tools/Map/Create Placeholder Art And Fallback")]
    public static void CreatePlaceholderArtAndFallback()
    {
        if (Directory.Exists(BaseFolder) == false)
        {
            Debug.LogError($"[MapToolbox] '{BaseFolder}' does not exist. Nothing to do.");
            return;
        }

        if (File.Exists(PlaceholderPath))
            Debug.Log($"[MapToolbox] Replacing the existing placeholder at {PlaceholderPath}");

        const int size = 1024;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Color baseParchment = new Color(0.716f, 0.682f, 0.604f, 1f);
        Color darkParchment = new Color(0.541f, 0.502f, 0.427f, 1f);
        Color frame = new Color(0.322f, 0.286f, 0.235f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float grain = Mathf.PerlinNoise(x * 0.021f, y * 0.021f) * 0.5f
                            + Mathf.PerlinNoise(x * 0.007f + 11.3f, y * 0.007f + 4.7f) * 0.5f;

                Color color = Color.Lerp(darkParchment, baseParchment, grain);

                int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                if (edge < 12) color = frame;
                else if (edge < 18) color = Color.Lerp(color, frame, 0.35f);

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        File.WriteAllBytes(PlaceholderPath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(PlaceholderPath, ImportAssetOptions.ForceUpdate);
        ConfigurePlaceholderImporter();

        Sprite placeholder = LoadSprite(PlaceholderPath);
        if (placeholder == null)
        {
            Debug.LogError($"[MapToolbox] Wrote {PlaceholderPath} but Unity did not import it as a Sprite. Check the importer.");
            return;
        }

        MapDefinition fallback = AssetDatabase.LoadAssetAtPath<MapDefinition>(FallbackPath);
        bool created = fallback == null;
        if (created) fallback = ScriptableObject.CreateInstance<MapDefinition>();

        SerializedObject so = new SerializedObject(fallback);
        so.FindProperty("sceneName").stringValue = "(fallback)";
        so.FindProperty("baseMap").objectReferenceValue = placeholder;
        so.FindProperty("calibrated").boolValue = false;
        so.FindProperty("placeholderLabel").stringValue = "Map coming soon";
        so.ApplyModifiedPropertiesWithoutUndo();

        if (created)
        {
            AssetDatabase.CreateAsset(fallback, FallbackPath);
            Debug.Log($"[MapToolbox] Created {FallbackPath}");
        }
        else
        {
            EditorUtility.SetDirty(fallback);
            Debug.Log($"[MapToolbox] Refreshed {FallbackPath}");
        }

        EditorUtility.SetDirty(placeholder);
        AssetDatabase.SaveAssets();
        Debug.Log($"[MapToolbox] Placeholder ready. Assign {FallbackPath} to the fallback slot of every MapSceneBinder.");
    }

    private static void ConfigurePlaceholderImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(PlaceholderPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[MapToolbox] No TextureImporter for {PlaceholderPath}.");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.isReadable = false;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();
    }

    /// <summary>
    /// Creates one MapDefinition per built scene, pre-filled with the drawing it belongs to and a
    /// first-pass world rectangle measured from the scene's static geometry, corrected to the
    /// drawing's aspect so the art never stretches.
    ///
    /// This is a starting point, not the calibration: verification item 3 in T-045 still needs a
    /// human to check a marker against a landmark at the centre and a far corner of each map.
    /// Existing definitions are left alone so hand-tuned numbers survive a re-run.
    /// </summary>
    [MenuItem("Tools/Map/Create Definitions For Built Scenes")]
    public static void CreateDefinitionsForBuiltScenes()
    {
        MapIconCatalog catalog = AssetDatabase.LoadAssetAtPath<MapIconCatalog>(CatalogPath);
        if (catalog == null)
            Debug.LogWarning("[MapToolbox] No icon catalog yet. Run Tools/Map/Seed Icon Catalog first.");

        int created = 0;
        int skipped = 0;

        for (int i = 0; i < SceneToDrawing.GetLength(0); i++)
        {
            string sceneName = SceneToDrawing[i, 0];
            string drawingName = SceneToDrawing[i, 1];

            Vector2 worldMin;
            Vector2 worldMax;
            Sprite drawing;

            string definitionPath = DefinitionPath(sceneName);
            if (AssetDatabase.LoadAssetAtPath<MapDefinition>(definitionPath) != null)
            {
                Debug.Log($"[MapToolbox] {definitionPath} already exists - leaving it alone.");
                skipped++;
                continue;
            }

            if (TryAuthorRect(sceneName, drawingName, out worldMin, out worldMax, out drawing) == false)
                continue;

            MapDefinition definition = ScriptableObject.CreateInstance<MapDefinition>();
            SerializedObject so = new SerializedObject(definition);
            so.FindProperty("sceneName").stringValue = sceneName;
            so.FindProperty("baseMap").objectReferenceValue = drawing;
            so.FindProperty("calibrated").boolValue = true;
            so.FindProperty("worldMin").vector2Value = worldMin;
            so.FindProperty("worldMax").vector2Value = worldMax;
            so.FindProperty("placeholderLabel").stringValue = string.Empty;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(definition, definitionPath);
            created++;
            Debug.Log($"[MapToolbox] {definitionPath}: world rect {worldMin} to {worldMax} ({(worldMax - worldMin).x:F0} x {(worldMax - worldMin).y:F0} m), drawing {drawingName} {drawing.rect.width}x{drawing.rect.height}px.");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[MapToolbox] Definitions created: {created}, already present: {skipped}. Next: check a marker against a landmark in each scene - these rectangles are a first pass, not a calibration.");
    }

    /// <summary>Measures a scene and fits the drawing's content box to it. Shared by create and reset.</summary>
    private static bool TryAuthorRect(string sceneName, string drawingName, out Vector2 worldMin, out Vector2 worldMax, out Sprite drawing)
    {
        worldMin = Vector2.zero;
        worldMax = Vector2.zero;

        drawing = LoadSprite(BaseFolder + "/" + drawingName + ".png");
        if (drawing == null)
        {
            Debug.LogError($"[MapToolbox] No drawing for {sceneName} at '{BaseFolder}/{drawingName}.png'.");
            return false;
        }

        string scenePath = $"Assets/Scenes/{sceneName}.unity";
        if (File.Exists(scenePath) == false)
        {
            Debug.LogError($"[MapToolbox] No scene at {scenePath}.");
            return false;
        }

        if (TryMeasureSceneBounds(scenePath, out worldMin, out worldMax) == false)
            return false;

        FitWorldRectToArtContent(ref worldMin, ref worldMax, drawing);
        return true;
    }

    /// <summary>DESTRUCTIVE: overwrites worldMin/worldMax on definitions a designer may have tuned by
    /// hand. It exists so an improved measurement can be applied to assets this tool generated
    /// earlier. Do not run it once anyone has calibrated a map against a landmark.</summary>
    [MenuItem("Tools/Map/RESET Definition Rectangles From Measurement")]
    public static void ResetDefinitionRectangles()
    {
        int updated = 0;

        for (int i = 0; i < SceneToDrawing.GetLength(0); i++)
        {
            string sceneName = SceneToDrawing[i, 0];
            MapDefinition definition = AssetDatabase.LoadAssetAtPath<MapDefinition>(DefinitionPath(sceneName));
            if (definition == null)
            {
                Debug.LogWarning($"[MapToolbox] No definition for {sceneName} at {DefinitionPath(sceneName)} - run Create Definitions first.");
                continue;
            }

            Vector2 worldMin;
            Vector2 worldMax;
            Sprite drawing;
            if (TryAuthorRect(sceneName, SceneToDrawing[i, 1], out worldMin, out worldMax, out drawing) == false)
                continue;

            SerializedObject so = new SerializedObject(definition);
            so.FindProperty("baseMap").objectReferenceValue = drawing;
            so.FindProperty("calibrated").boolValue = true;
            so.FindProperty("worldMin").vector2Value = worldMin;
            so.FindProperty("worldMax").vector2Value = worldMax;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);

            updated++;
            Debug.Log($"[MapToolbox] {sceneName}: rectangle is now {worldMin} to {worldMax} ({(worldMax - worldMin).x:F0} x {(worldMax - worldMin).y:F0} m).");
        }

        AssetDatabase.SaveAssets();
        Debug.LogWarning($"[MapToolbox] Reset {updated} rectangle(s) from raw renderer bounds. This OVERWRITES hand-calibrated values (T-045 step E) - if a map was already aligned to its landmarks, undo before you leave the editor. Still first passes: verify a marker against a landmark in every scene (T-045 verification 3).");
    }

    private static string DefinitionPath(string sceneName)
    {
        const string folder = MapRoot + "/Definitions";
        if (Directory.Exists(folder) == false)
            AssetDatabase.CreateFolder(MapRoot, "Definitions");

        return $"{folder}/Map_{sceneName}.asset";
    }

    /// <summary>The opaque part of a drawing, in normalized sprite UVs. These maps are stylised
    /// islands floating on transparency, so the level's world rectangle corresponds to the island,
    /// not to the sprite rect - mapping it to the full rect slides every marker off its landmark.</summary>
    internal static bool TryGetArtContentRect(Sprite art, out Vector2 contentMin, out Vector2 contentMax)
    {
        contentMin = Vector2.zero;
        contentMax = Vector2.one;

        Texture source = art == null ? null : art.texture;
        if (source == null) return false;

        const int sample = 256;
        int w = sample;
        int h = Mathf.Max(1, Mathf.RoundToInt((float)source.height * sample / source.width));

        RenderTexture previous = RenderTexture.active;
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;
        Texture2D read = new Texture2D(w, h, TextureFormat.RGBA32, false);
        read.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        read.Apply();
        RenderTexture.ReleaseTemporary(rt);
        RenderTexture.active = previous;

        Color32[] pixels = read.GetPixels32();
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        int opaque = 0;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].a <= 8) continue;
                opaque++;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        UnityEngine.Object.DestroyImmediate(read);

        if (opaque < w * h / 100)
        {
            Debug.LogError($"[MapToolbox] '{art.name}' is almost entirely transparent - cannot find its content box.");
            return false;
        }

        contentMin = new Vector2((float)minX / w, (float)minY / h);
        contentMax = new Vector2((float)(maxX + 1) / w, (float)(maxY + 1) / h);
        return true;
    }

    /// <summary>Solves for the world rectangle that (a) has exactly the aspect of the drawing, so
    /// <see cref="MapDefinition.Validate"/> stays quiet and the art is never stretched, and (b) whose
    /// <em>content</em> box - the opaque island, not the sprite rect - contains the measured level
    /// bounds with the island's centre on the level's centre.</summary>
    ///
    /// <remarks>Two bugs lived here, and both put every marker off its landmark. (1) The version it
    /// replaces aspect-fitted the rectangle and THEN grew each axis independently with Mathf.Max,
    /// which destroyed the aspect it had just established: the rectangles it wrote were 5-10% out of
    /// square against their own art (measured 11/12/2026 - Village 10.3%, Party 7.6%, FirstForest
    /// 5.5%, SpaceBoss 5.6%; only Forest's 1.1% slipped under the 2% gate, which is why this looked
    /// like it worked). Growing must be done as one scale factor, never per axis. (2) It centred the
    /// full sprite rect on the level; the island is what depicts the level, and the island is not
    /// centred in these sprites, so it biased the whole map by half the island's offset.</remarks>
    private static void FitWorldRectToArtContent(ref Vector2 worldMin, ref Vector2 worldMax, Sprite art)
    {
        float artAspect = art == null || art.rect.height <= 0f ? 1f : (float)art.rect.width / art.rect.height;

        if (TryGetArtContentRect(art, out Vector2 contentMin, out Vector2 contentMax) == false)
        {
            RectAspectFit(ref worldMin, ref worldMax, artAspect);
            return;
        }

        Vector2 measuredCenter = (worldMin + worldMax) * 0.5f;
        Vector2 measured = worldMax - worldMin;
        Vector2 fraction = contentMax - contentMin;
        if (fraction.x <= 0.001f || fraction.y <= 0.001f || artAspect <= 0f)
        {
            RectAspectFit(ref worldMin, ref worldMax, artAspect);
            return;
        }

        // The island occupies `fraction` of the sprite, so to cover `measured` with the island the
        // sprite needs reqX x reqY metres. Take whichever requirement is binding and rebuild the
        // other from the art aspect - one scale factor, so the result is square to the drawing.
        Vector2 required = new Vector2(measured.x / fraction.x, measured.y / fraction.y);
        float spanX = Mathf.Max(required.x, artAspect * required.y);
        Vector2 span = new Vector2(spanX, spanX / artAspect);

        // Anchor by the island's own centre inside the sprite rect, not by the rect's centre.
        Vector2 islandCenterUv = (contentMin + contentMax) * 0.5f;
        worldMin = measuredCenter - new Vector2(islandCenterUv.x * span.x, islandCenterUv.y * span.y);
        worldMax = worldMin + span;

        Debug.Log($"[MapToolbox] '{art.name}' content box uv {contentMin} to {contentMax} ({fraction.x * 100f:F0}% x {fraction.y * 100f:F0}% of the rect, centre {islandCenterUv}); world rect {measured:F0} m measured -> {span:F0} m authored, aspect-locked to {artAspect:F4}.");
    }

    /// <summary>Measures a scene's XZ extents. If the scene is already open in the editor it is
    /// measured as it stands - it is never closed or reloaded, because the user may have unsaved
    /// work in it. Otherwise it is opened additively, measured and closed without saving.
    /// internal: MapCalibrationFitting fits against the same measurement rather than a second
    /// implementation that could drift from this one.</summary>
    internal static bool TryMeasureSceneBounds(string scenePath, out Vector2 worldMin, out Vector2 worldMax)
    {
        worldMin = Vector2.zero;
        worldMax = Vector2.zero;

        Scene already = GetLoadedScene(scenePath);
        bool openedHere = false;

        if (already.IsValid())
        {
            Debug.Log($"[MapToolbox] {scenePath} is already open - measuring it as it stands.");
        }
        else
        {
            already = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            if (already.IsValid() == false)
            {
                Debug.LogError($"[MapToolbox] Could not open {scenePath} additively.");
                return false;
            }

            openedHere = true;
        }

        bool any = false;
        int considered = 0;
        int skippedOversized = 0;
        float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;

        GameObject[] roots = already.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            Renderer[] renderers = roots[r].GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                // These levels are not static-flagged, so the layer is the reliable filter: HUD and
                // overlay art must not define where the world ends.
                int layer = renderer.gameObject.layer;
                if (layer == LayerMask.NameToLayer("UI")) continue;
                if (renderer is ParticleSystemRenderer) continue;

                // A Canvas that is NOT on the UI layer still contributes CanvasRenderer bounds, and those
                // are in screen units around the origin - which is how every scene's measured rectangle
                // reached for z = 0 and changed between runs depending on whether the HUD had drawn.
                // Measuring "where the world ends" cannot include the widget layer, whatever layer it is.
                if (renderer is CanvasRenderer) continue;

                Bounds bounds = renderer.bounds;

                // One stray skybox dome or distant prop would flatten the whole calibration, so
                // anything absurdly large is reported instead of trusted.
                if (bounds.size.x > 4000f || bounds.size.z > 4000f)
                {
                    skippedOversized++;
                    continue;
                }

                considered++;
                any = true;
                minX = Mathf.Min(minX, bounds.min.x);
                minZ = Mathf.Min(minZ, bounds.min.z);
                maxX = Mathf.Max(maxX, bounds.max.x);
                maxZ = Mathf.Max(maxZ, bounds.max.z);
            }
        }

        if (openedHere)
            EditorSceneManager.CloseScene(already, true);

        if (any == false)
        {
            Debug.LogError($"[MapToolbox] No usable renderer found in {scenePath}. Author the world rectangle by hand.");
            return false;
        }

        if (skippedOversized > 0)
            Debug.LogWarning($"[MapToolbox] {scenePath}: ignored {skippedOversized} oversized renderer(s) larger than 4000 m across.");

        Debug.Log($"[MapToolbox] {scenePath}: measured {considered} renderers.");

        worldMin = new Vector2(minX, minZ);
        worldMax = new Vector2(maxX, maxZ);
        return true;
    }

    private static Scene GetLoadedScene(string scenePath)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene candidate = SceneManager.GetSceneAt(i);
            if (candidate.isLoaded && candidate.path == scenePath) return candidate;
        }

        return default;
    }

    /// <summary>Grows the shorter axis so the world rectangle has the same aspect as the art. The
    /// alternative - letting the Image stretch - is exactly what MapDefinition.Validate warns about,
    /// because it slides markers off the landmarks they belong to.</summary>
    private static void RectAspectFit(ref Vector2 worldMin, ref Vector2 worldMax, float artAspect)
    {
        Vector2 center = (worldMin + worldMax) * 0.5f;
        Vector2 span = worldMax - worldMin;
        if (span.x <= 0f || span.y <= 0f || artAspect <= 0f) return;

        float worldAspect = span.x / span.y;
        if (worldAspect < artAspect) span.x = span.y * artAspect;
        else span.y = span.x / artAspect;

        worldMin = center - span * 0.5f;
        worldMax = center + span * 0.5f;
    }

    /// <summary>D-11 Q6: the five base drawings go to 4096 with mipmaps on and normal-quality
    /// compression. They are downsampled to roughly 0.3x on the big map, which is exactly when a
    /// sprite without mipmaps shimmers. The 20 icons in Elements/ are deliberately left alone.</summary>
    [MenuItem("Tools/Map/Raise Base Maps To 4096")]
    public static void RaiseBaseMapsTo4096()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { BaseFolder });
        int changed = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.EndsWith(".png") == false) continue;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool needsChange = importer.maxTextureSize < 4096 || importer.mipmapEnabled == false
                             || importer.textureCompression != TextureImporterCompression.Compressed;
            if (needsChange == false)
            {
                Debug.Log($"[MapToolbox] {path} already at 4096 / mipmaps / compressed.");
                continue;
            }

            importer.maxTextureSize = 4096;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = false;
            importer.streamingMipmaps = false;
            importer.SaveAndReimport();
            changed++;
            Debug.Log($"[MapToolbox] Re-imported {path} at 4096, mipmaps on, compressed.");
        }

        Debug.Log($"[MapToolbox] Base map import settings applied to {changed} drawing(s). Memory note (D-11 Q6): one scene's map is resident at a time, roughly 28 MB each with the mip chain.");
    }

    /// <summary>Runs every MapDefinition's own checks and prints a summary, so a bad calibration is
    /// caught in the editor instead of looking like a haunted marker in play mode.</summary>
    [MenuItem("Tools/Map/Validate All Definitions")]
    public static void ValidateAllDefinitions()
    {
        string[] guids = AssetDatabase.FindAssets("t:MapDefinition", new[] { MapRoot });
        int problems = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            MapDefinition definition = AssetDatabase.LoadAssetAtPath<MapDefinition>(path);
            if (definition == null)
            {
                Debug.LogError($"[MapToolbox] {path} does not load as a MapDefinition.");
                problems++;
                continue;
            }

            problems += definition.Validate();
        }

        MapIconCatalog catalog = AssetDatabase.LoadAssetAtPath<MapIconCatalog>(CatalogPath);
        if (catalog == null)
        {
            Debug.LogError($"[MapToolbox] No icon catalog at {CatalogPath}. Run Tools/Map/Seed Icon Catalog.");
            problems++;
        }
        else
        {
            for (int t = 0; t < IconOrder.Length; t++)
            {
                if (catalog.Has(IconOrder[t])) continue;
                Debug.LogError($"[MapToolbox] Icon catalog has no drawing for {IconOrder[t]}.");
                problems++;
            }
        }

        Debug.Log(problems == 0
            ? $"[MapToolbox] Map validation clean: {guids.Length} definition(s), {IconOrder.Length} icon types all resolving."
            : $"[MapToolbox] Map validation found {problems} problem(s) across {guids.Length} definition(s).");
    }
}
