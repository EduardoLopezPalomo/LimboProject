using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// T-045 step E: turns map calibration from "walk around and eyeball it" into a measurement.
///
/// Per scene, in one run:
///  1. captures a top-down orthographic render of the level's static geometry - no characters, no UI,
///     no particles - so the real ground is what gets compared;
///  2. reads the art drawing's opaque island as a mask (the drawings float on transparency);
///  3. hill-climbs centre, scale and rotation for the rectangle whose island overlaps the real geometry
///     best, always keeping that rectangle's aspect EQUAL to the drawing's, so the 2 % gate in
///     MapDefinition.Validate cannot fire on anything this tool writes;
///  4. writes a draft, a composite sheet PNG (capture + drawing at half alpha + a metre grid + every
///     marker as a numbered dot) and a text report, all for reading with eyes;
///  5. writes nothing into the project until Apply Drafts To Definitions runs.
///
/// Everything but step 5 goes to Library/MapCalibration, which Unity regenerates and git ignores, so
/// iterating costs the repo nothing. The only assets this tool can change are the five MapDefinition
/// assets, and only through that one menu item.
///
/// The overlap score is a guide, not a verdict: the drawings are stylised, so a high score means the
/// island covers the level correctly. The sheet is what shows whether the painted roads actually sit
/// on the real ones - nudge the draft numbers and re-run Render Sheets From Drafts until they do.
///
/// Menus:
/// Tools/Map/Calibration/Capture And Fit (All 5 Scenes)
/// Tools/Map/Calibration/Capture And Fit (Current Scene)
/// Tools/Map/Calibration/Render Sheets From Drafts
/// Tools/Map/Calibration/Apply Drafts To Definitions
/// Tools/Map/Calibration/Delete Drafts And Captures
/// </summary>
internal static class MapCalibrationFitting
{
    private const string ScenesFolder = "Assets/Scenes/";
    private const string DefinitionsFolder = "Assets/UI Images/Map/Definitions";
    private const string OutFolder = "Library/MapCalibration";

    private const int MaskGrid = 168;
    private const int CaptureMaxEdge = 2048;
    private const int SheetMaxEdge = 1500;

    /// <summary>The drawings are authored north-up (D-11), so a fit that is accepted without a human
    /// saying so may only turn this far. An unconstrained hill climb will happily rotate a blob until
    /// its overlap score improves, which measures well and puts every landmark in the wrong place.</summary>
    private const float NorthLimit = 15f;

    private static readonly string[] BuiltScenes = { "FirstForest", "Forest", "Village", "Party", "SpaceBoss" };

    /// <summary>Every orientation the fit will consider. The art was authored north-up per D-11, but at
    /// least two drawings were exported a quarter turn from the level, so 0 is a candidate rather than an
    /// assumption - and a near-round island or a river across the middle cannot tell 0 from 180, which is
    /// precisely why a single-orientation check has to sweep instead of trusting one number.</summary>
    private static readonly float[] CardinalAngles = { 0f, 90f, 180f, 270f };

    /// <summary>How much better another orientation must be, in absolute overlap points, before the fit
    /// turns the art at all. Keeps a 0.2-point coin flip on a near-square footprint from becoming an
    /// authored 180.</summary>
    private const float RotationMinGain = 0.02f;

    /// <summary>The layers that depict ground: Default, Water, Wall, Floor, Mountain. Deliberately
    /// leaves out Player / EnemyLayer / Shield / Proyectile (moving things), UI, TransparentFX and
    /// MinimapIcon (the scrapped overlay layer), because a walking citizen is not a landmark.</summary>
    private static readonly int[] GeometryLayers = { 0, 4, 6, 12, 13 };

    private class Draft
    {
        public Vector2 worldMin;
        public Vector2 worldMax;
        public float north;
        public float scoreBefore;
        public float scoreAfter;
        public Vector2 measuredMin;
        public Vector2 measuredMax;
        public Vector2 captureMin;
        public Vector2 captureMax;
        public int captureWidth;
        public int captureHeight;
        public string note = string.Empty;
    }

    private class Mask
    {
        public int width;
        public int height;
        public bool[] land;
        public Color32[] colour;
        public Vector2 worldMin;
        public Vector2 worldMax;

        public bool IsLand(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return false;
            return land[y * width + x];
        }
    }

    private class MarkerInfo
    {
        public string label;
        public Vector3 world;
        public string icon;
        public string tier;
        public bool isPlayer;
        public bool outside;
        public Vector2 mapPoint;
        public Vector2 sheetPixel;
    }

    [MenuItem("Tools/Map/Calibration/Capture And Fit (All 5 Scenes)")]
    private static void FitAll()
    {
        Scene current = EditorSceneManager.GetActiveScene();
        string returnPath = current.path;
        if (current.isDirty)
        {
            Debug.LogError("[MapCalib] The open scene has unsaved changes. Save or revert it first: this tool opens scenes single-mode and would discard your work.");
            return;
        }

        EnsureOutFolder();

        int done = 0;
        for (int i = 0; i < BuiltScenes.Length; i++)
            if (ProcessScene(BuiltScenes[i])) done++;

        if (string.IsNullOrEmpty(returnPath) == false)
            EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);

        Debug.Log($"[MapCalib] Fitted {done}/{BuiltScenes.Length} scene(s). Read {OutFolder}/<scene>_sheet.png for each before Apply Drafts To Definitions.");
    }

    [MenuItem("Tools/Map/Calibration/Capture And Fit (Current Scene)")]
    private static void FitCurrent()
    {
        Scene current = EditorSceneManager.GetActiveScene();
        string returnPath = current.path;
        if (current.isDirty)
        {
            Debug.LogError("[MapCalib] The open scene has unsaved changes. Save or revert it first: the capture reopens the scene from disk.");
            return;
        }

        string name = current.name;
        if (System.Array.IndexOf(BuiltScenes, name) < 0)
        {
            Debug.LogError($"[MapCalib] '{name}' is not one of the five mapped scenes ({string.Join(", ", BuiltScenes)}).");
            return;
        }

        EnsureOutFolder();
        ProcessScene(name);
        if (string.IsNullOrEmpty(returnPath) == false) EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
    }

    private static bool ProcessScene(string sceneName)
    {
        string scenePath = ScenesFolder + sceneName + ".unity";
        if (File.Exists(scenePath) == false)
        {
            Debug.LogError($"[MapCalib] No scene at {scenePath}.");
            return false;
        }

        MapDefinition def = AssetDatabase.LoadAssetAtPath<MapDefinition>(DefinitionsFolder + "/Map_" + sceneName + ".asset");
        if (def == null || def.BaseMap == null)
        {
            Debug.LogError($"[MapCalib] {sceneName} has no MapDefinition with a drawing, so there is nothing to fit.");
            return false;
        }

        Scene opened = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (opened.IsValid() == false)
        {
            Debug.LogError($"[MapCalib] Could not open {scenePath}.");
            return false;
        }

        Draft draft = new Draft();

        if (MapToolbox.TryMeasureSceneBounds(scenePath, out Vector2 measuredMin, out Vector2 measuredMax) == false)
        {
            Debug.LogError($"[MapCalib] {sceneName}: could not measure the level bounds.");
            return false;
        }

        draft.measuredMin = measuredMin;
        draft.measuredMax = measuredMax;

        if (CaptureTopDown(sceneName, measuredMin, measuredMax, draft) == false) return false;

        Mask art = ReadArtMask(def.BaseMap);
        Mask cap = ReadCaptureMask(sceneName, draft);
        if (art == null || cap == null) return false;

        float artAspect = (float)def.BaseMap.rect.width / def.BaseMap.rect.height;

        // What the rectangle is fitted to, and why. The user's answer on 11/12 is that the drawings are
        // built but stylised, so the built footprint is the target and the land-overlap score is a
        // reading taken afterwards, not the thing being maximised. Chasing the score instead of the
        // footprint is what asked for a 15 degree rotation on a blob and left Village sitting 82 m south
        // of its own level.
        bool haveGround = TryMeasureGroundFootprint(out Vector2 targetMin, out Vector2 targetMax);
        if (haveGround == false)
        {
            targetMin = measuredMin;
            targetMax = measuredMax;
        }

        draft.scoreBefore = Score(def.WorldMin, def.WorldMax, def.MapNorthDegrees, art, cap);

        // Try all four cardinal orientations and keep the best. Pinning north to 0 was my mistake: some
        // of this art was exported a quarter turn from the level layout (Village and SpaceBoss both score
        // 12-20 points better at 270), and nothing about a single-orientation composite shows that - an
        // island that is roughly round, or a river that crosses the middle, overlaps itself after half a
        // turn, so "the landmarks line up" cannot distinguish 0 from 180. Sweep, then look.
        float bestScore = float.MinValue;
        Vector2 bestMin = targetMin;
        Vector2 bestMax = targetMax;
        float bestNorth = 0f;
        float zeroScore = float.MinValue;
        Vector2 zeroMin = targetMin;
        Vector2 zeroMax = targetMax;
        System.Text.StringBuilder rotationTable = new System.Text.StringBuilder();
        for (int a = 0; a < CardinalAngles.Length; a++)
        {
            RotateRect(targetMin, targetMax, CardinalAngles[a], out Vector2 rotatedMin, out Vector2 rotatedMax);
            IslandFit(rotatedMin, rotatedMax, def.BaseMap, artAspect, out Vector2 candMin, out Vector2 candMax);
            float candScore = Score(candMin, candMax, CardinalAngles[a], art, cap);
            rotationTable.Append(string.Format(CultureInfo.InvariantCulture,
                " north {0:F0}: {1:F1}%", CardinalAngles[a], candScore * 100f));

            if (CardinalAngles[a] == 0f)
            {
                zeroScore = candScore;
                zeroMin = candMin;
                zeroMax = candMax;
            }

            if (candScore > bestScore)
            {
                bestScore = candScore;
                bestMin = candMin;
                bestMax = candMax;
                bestNorth = CardinalAngles[a];
            }
        }

        // A quarter turn has to earn its place. On a near-square footprint (Party 213 x 206, Forest
        // 948 x 842) the four orientations land within a point of each other and the winner is noise -
        // Party picked 180 by 0.2, which is not evidence of anything. Turn the art only when the gain is
        // a real gap; otherwise keep the orientation the art was authored in.
        if (bestNorth != 0f && bestScore < zeroScore + RotationMinGain)
        {
            rotationTable.Append(string.Format(CultureInfo.InvariantCulture,
                "  [turning to {0:F0} gains only {1:F1} pts, below the {2:F0} pt bar - staying at 0]",
                bestNorth, (bestScore - zeroScore) * 100f, RotationMinGain * 100f));
            bestNorth = 0f;
            bestScore = zeroScore;
            bestMin = zeroMin;
            bestMax = zeroMax;
        }

        draft.worldMin = bestMin;
        draft.worldMax = bestMax;
        draft.north = bestNorth;
        draft.scoreAfter = bestScore;

        // Recorded so the trade is visible to whoever reads this next, and never accepted: what the
        // IoU hill climb would have chosen, with rotation capped and with it free.
        FitRect(def.WorldMin, def.WorldMax, def.MapNorthDegrees, targetMin, targetMax, art, cap, artAspect,
                out Vector2 climbMin, out Vector2 climbMax, out float climbNorth, out float climbScore);

        Vector2 freeMin = climbMin;
        Vector2 freeMax = climbMax;
        float freeNorth = climbNorth;
        float freeScore = climbScore;
        Climb(climbMin, climbMax, climbNorth, 180f, art, cap, ref freeMin, ref freeMax, ref freeNorth, ref freeScore);

        draft.note = string.Format(CultureInfo.InvariantCulture,
            "fitted to the built footprint {0:F0}x{1:F0} m at centre {2:F0},{3:F0} (all-renderer bounds {4:F0}x{5:F0} m).{6}; chosen north {7:F0} (overlap {8:F1}% vs {9:F1}% as-shipped). The continuous IoU climb would prefer {10:F0}x{11:F0} m at north {12:F1} ({13:F1}%), or north {14:F1} ({15:F1}%) with rotation free; recorded, not applied, because the art is stylised and a free rotation can invent overlap.",
            (targetMax - targetMin).x, (targetMax - targetMin).y,
            (targetMin.x + targetMax.x) * 0.5f, (targetMin.y + targetMax.y) * 0.5f,
            (measuredMax - measuredMin).x, (measuredMax - measuredMin).y,
            rotationTable, bestNorth, bestScore * 100f, draft.scoreBefore * 100f,
            (climbMax - climbMin).x, (climbMax - climbMin).y, climbNorth, climbScore * 100f,
            freeNorth, freeScore * 100f);

        WriteDraft(sceneName, draft);

        List<MarkerInfo> markers = CollectMarkers(draft);
        RenderSheet(sceneName, draft, art, cap, BuildCaptureLayer(sceneName, draft), markers, string.Empty);
        WriteReport(sceneName, draft, art, cap, markers, def);

        Vector2 oldSpan = def.WorldMax - def.WorldMin;
        Vector2 newSpan = draft.worldMax - draft.worldMin;
        Vector2 oldCenter = (def.WorldMin + def.WorldMax) * 0.5f;
        Vector2 newCenter = (draft.worldMin + draft.worldMax) * 0.5f;
        Debug.Log($"[MapCalib] {sceneName}: footprint {(targetMax - targetMin).x:F0}x{(targetMax - targetMin).y:F0} m -> rect {oldSpan.x:F0}x{oldSpan.y:F0} -> {newSpan.x:F0}x{newSpan.y:F0} m, centre moved {(newCenter - oldCenter).magnitude:F0} m to {newCenter.x:F0},{newCenter.y:F0}; overlap {draft.scoreBefore * 100f:F1}% -> {draft.scoreAfter * 100f:F1}%; markers {markers.Count}. Sheet {OutFolder}/{sceneName}_sheet.png");
        return true;
    }

    /// <summary>Top-down orthographic capture of the ground layers only. The camera is created, used
    /// and destroyed here; the scene is never marked dirty and nothing is saved.</summary>
    private static bool CaptureTopDown(string sceneName, Vector2 measuredMin, Vector2 measuredMax, Draft draft)
    {
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        bool anyY = false;

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            Renderer[] renderers = roots[r].GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer ren = renderers[i];
                if (IsGeometryLayer(ren.gameObject.layer) == false) continue;
                if (ren is ParticleSystemRenderer) continue;
                Bounds b = ren.bounds;
                if (b.size.x > 4000f || b.size.z > 4000f) continue;
                anyY = true;
                if (b.min.y < minY) minY = b.min.y;
                if (b.max.y > maxY) maxY = b.max.y;
            }
        }

        if (anyY == false)
        {
            Debug.LogError($"[MapCalib] {sceneName}: no renderer sits on a ground layer, so the capture has nothing to show.");
            return false;
        }

        Vector2 span = measuredMax - measuredMin;
        if (span.x <= 0f || span.y <= 0f)
        {
            Debug.LogError($"[MapCalib] {sceneName}: the measured bounds are empty.");
            return false;
        }

        float pxPerMetre = Mathf.Min((float)CaptureMaxEdge / Mathf.Max(span.x, span.y), 8f);
        int w = Mathf.Max(64, Mathf.RoundToInt(span.x * pxPerMetre));
        int h = Mathf.Max(64, Mathf.RoundToInt(span.y * pxPerMetre));

        float highMargin = Mathf.Max(60f, (maxY - minY) + 30f);
        float camY = maxY + highMargin;

        GameObject go = new GameObject("MapCalibrationCapture");
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = span.y * 0.5f;
        cam.aspect = (float)w / h;
        cam.nearClipPlane = 1f;
        cam.farClipPlane = highMargin + (maxY - minY) + 100f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.allowHDR = false;
        cam.allowMSAA = false;
        cam.useOcclusionCulling = false;
        cam.depthTextureMode = DepthTextureMode.None;
        cam.cullingMask = 0;
        for (int i = 0; i < GeometryLayers.Length; i++) cam.cullingMask |= 1 << GeometryLayers[i];

        // LookRotation(down, +Z) instead of Euler(-90,0,0): this is the orientation whose image right is
        // world +X (east) and whose image up is world +Z (north) - exactly how map space is defined in
        // MapDefinition.WorldToMapPoint. Any other choice mirrors or rotates the whole capture and every
        // conclusion drawn from it.
        go.transform.position = new Vector3((measuredMin.x + measuredMax.x) * 0.5f, camY, (measuredMin.y + measuredMax.y) * 0.5f);
        go.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;

        byte[] png = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);
        cam.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(go);

        File.WriteAllBytes(OutFolder + "/" + sceneName + "_topdown.png", png);

        draft.captureWidth = w;
        draft.captureHeight = h;
        draft.captureMin = measuredMin;
        draft.captureMax = measuredMax;
        return true;
    }

    /// <summary>The drawing's opaque island as a mask, plus its colour for the composite. Read by
    /// blitting through an RT because the base maps are imported with isReadable off - the same trick
    /// MapToolbox.TryGetArtContentRect uses. The five drawings are Single sprites with no padding, so
    /// the sprite rect is the whole texture, which is what WorldToMapPoint assumes.</summary>
    private static Mask ReadArtMask(Sprite art)
    {
        Texture source = art == null ? null : art.texture;
        if (source == null)
        {
            Debug.LogError("[MapCalib] The drawing has no texture, so it cannot be masked.");
            return null;
        }

        const int sample = 880;
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
        UnityEngine.Object.DestroyImmediate(read);

        Mask m = new Mask { width = w, height = h, colour = pixels, land = new bool[w * h] };
        for (int i = 0; i < pixels.Length; i++) m.land[i] = pixels[i].a > 24 && IsWater(pixels[i]) == false;
        return m;
    }

    /// <summary>The capture downsampled to the scoring grid. A cell is ground when most of the source
    /// pixels under it are opaque, so anti-aliased edges and long thin props do not inflate it.</summary>
    private static Mask ReadCaptureMask(string sceneName, Draft draft)
    {
        string path = OutFolder + "/" + sceneName + "_topdown.png";
        if (File.Exists(path) == false)
        {
            Debug.LogError($"[MapCalib] No capture at {path}.");
            return null;
        }

        Texture2D loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (ImageConversion.LoadImage(loaded, File.ReadAllBytes(path)) == false)
        {
            Debug.LogError($"[MapCalib] {path} does not decode.");
            UnityEngine.Object.DestroyImmediate(loaded);
            return null;
        }

        int sw = loaded.width;
        int sh = loaded.height;
        Color32[] src = loaded.GetPixels32();
        UnityEngine.Object.DestroyImmediate(loaded);

        int gw = MaskGrid;
        int gh = Mathf.Max(1, Mathf.RoundToInt((float)sh * MaskGrid / sw));
        bool[] land = new bool[gw * gh];

        for (int gy = 0; gy < gh; gy++)
        {
            int y0 = gy * sh / gh;
            int y1 = Mathf.Max(y0 + 1, (gy + 1) * sh / gh);
            for (int gx = 0; gx < gw; gx++)
            {
                int x0 = gx * sw / gw;
                int x1 = Mathf.Max(x0 + 1, (gx + 1) * sw / gw);
                int opaque = 0;
                int total = 0;
                for (int y = y0; y < y1; y += 2)
                    for (int x = x0; x < x1; x += 2)
                    {
                        total++;
                        Color32 s = src[y * sw + x];
                        if (s.a > 24 && IsWater(s) == false) opaque++;
                    }

                if (total > 0 && (float)opaque / total > 0.35f) land[gy * gw + gx] = true;
            }
        }

        return new Mask
        {
            width = gw,
            height = gh,
            land = land,
            worldMin = draft.captureMin,
            worldMax = draft.captureMax
        };
    }

    /// <summary>Sea and lake blue, on either side of the comparison.
    ///
    /// This is the whole reason the first fit was wrong. In the capture the level is an island ringed by a
    /// big Water layer quad, so "the pixel is opaque" is true across the entire rectangle the water plane
    /// covers - the mask was a square, and the hill climb scored best by growing the world rectangle to
    /// swallow it and rotating the blob for free overlap. The drawings have the same coastline and the
    /// same lake, painted blue, so the symmetric rule - opaque AND not water-blue - compares land against
    /// land on both sides, which is the thing that actually has to line up.</summary>
    private static bool IsWater(Color32 c)
    {
        return c.b > 150 && c.b > c.r + 40;
    }

    /// <summary>The bounding box of a mask's opaque pixels in normalized uv, i.e. where the island
    /// sits inside the sprite. Used only for a starting guess - MapToolbox owns the authoritative
    /// version of this measurement.</summary>
    private static void ContentUv(Mask m, out Vector2 cMin, out Vector2 cMax)
    {
        int minX = m.width, minY = m.height, maxX = -1, maxY = -1;
        for (int y = 0; y < m.height; y++)
            for (int x = 0; x < m.width; x++)
            {
                if (m.land[y * m.width + x] == false) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

        if (maxX < 0) { cMin = Vector2.zero; cMax = Vector2.one; return; }

        cMin = new Vector2((float)minX / m.width, (float)minY / m.height);
        cMax = new Vector2((float)(maxX + 1) / m.width, (float)(maxY + 1) / m.height);
    }

    private static Vector2 Rotate(Vector2 value, float cos, float sin)
    {
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }

    /// <summary>Where a world point lands inside a candidate drawing rectangle, in sprite UVs with y
    /// up. Deliberately the same maths as MapDefinition.WorldToMapPoint: rotate the offset about the
    /// rect centre by -north. If these two ever disagree, the sheet lies about what the game draws.</summary>
    private static bool TryArtUv(Vector3 world, Vector2 center, Vector2 span, float north, out Vector2 uv)
    {
        float radians = -north * Mathf.Deg2Rad;
        Vector2 offset = Rotate(new Vector2(world.x - center.x, world.z - center.y), Mathf.Cos(radians), Mathf.Sin(radians));
        uv = new Vector2(0.5f + offset.x / span.x, 0.5f + offset.y / span.y);
        return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
    }

    /// <summary>Intersection over union of "the drawing has paint here" and "the level has ground
    /// here", walked over the capture grid so the level is the fixed frame of reference. Cells that are
    /// neither ground nor inside the candidate rect are skipped - they would swamp the score with
    /// agreement about empty space. Both miss-types cost: a rect too small leaves ground uncovered, a
    /// rect too big claims paint over nothing.</summary>
    private static float Score(Vector2 rectMin, Vector2 rectMax, float north, Mask art, Mask cap)
    {
        Vector2 center = (rectMin + rectMax) * 0.5f;
        Vector2 span = rectMax - rectMin;
        if (span.x <= 0f || span.y <= 0f) return 0f;

        float radians = -north * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        Vector2 capSpan = cap.worldMax - cap.worldMin;
        int both = 0;
        int either = 0;

        for (int y = 0; y < cap.height; y++)
        {
            float worldZ = cap.worldMin.y + (y + 0.5f) / cap.height * capSpan.y;
            for (int x = 0; x < cap.width; x++)
            {
                bool capLand = cap.land[y * cap.width + x];
                Vector2 offset = new Vector2(cap.worldMin.x + (x + 0.5f) / cap.width * capSpan.x - center.x, worldZ - center.y);
                Vector2 rotated = Rotate(offset, cos, sin);
                float u = 0.5f + rotated.x / span.x;
                float v = 0.5f + rotated.y / span.y;
                bool inside = u >= 0f && u <= 1f && v >= 0f && v <= 1f;

                bool artLand = false;
                if (inside)
                {
                    int ax = Mathf.Clamp((int)(u * art.width), 0, art.width - 1);
                    int ay = Mathf.Clamp((int)(v * art.height), 0, art.height - 1);
                    artLand = art.land[ay * art.width + ax];
                }

                if (capLand == false && inside == false) continue;
                if (capLand || artLand) either++;
                if (capLand && artLand) both++;
            }
        }

        return either == 0 ? 0f : (float)both / either;
    }

    /// <summary>Hill-climbs centre, scale and rotation to maximise island-over-ground overlap. Scale is
    /// uniform on purpose: the rectangle's aspect stays pinned to the drawing's, so nothing written from
    /// here can stretch the art or trip MapDefinition.Validate. Two starts are tried - the rect the asset
    /// carries now, and a fresh aspect-locked island fit of the raw measured bounds - and whichever ends
    /// up scoring better wins, so a hand calibration that is already good is never lost to a gradient.</summary>
    private static void FitRect(Vector2 curMin, Vector2 curMax, float curNorth, Vector2 measuredMin, Vector2 measuredMax,
                                Mask art, Mask cap, float artAspect,
                                out Vector2 bestMin, out Vector2 bestMax, out float bestNorth, out float bestScore)
    {
        ContentUv(art, out Vector2 islandMin, out Vector2 islandMax);
        Vector2 fraction = islandMax - islandMin;
        if (fraction.x <= 0.001f || fraction.y <= 0.001f) fraction = Vector2.one;
        Vector2 islandCenterUv = (islandMin + islandMax) * 0.5f;

        // Start 1: the same island fit the fixed MapToolbox solve produces, from raw bounds.
        Vector2 measuredCenter = (measuredMin + measuredMax) * 0.5f;
        Vector2 measured = measuredMax - measuredMin;
        Vector2 required = new Vector2(measured.x / fraction.x, measured.y / fraction.y);
        float freshSpanX = Mathf.Max(required.x, artAspect * required.y);
        Vector2 freshSpan = new Vector2(freshSpanX, freshSpanX / artAspect);
        Vector2 freshMin = measuredCenter - new Vector2(islandCenterUv.x * freshSpan.x, islandCenterUv.y * freshSpan.y);

        // Start 2: today's authored rect, aspect-corrected the same way, because the point of climbing
        // from it is to keep whatever it already got right.
        Vector2 curCenter = (curMin + curMax) * 0.5f;
        Vector2 curSpan = curMax - curMin;
        if (curSpan.y * artAspect > curSpan.x) curSpan.x = curSpan.y * artAspect;
        else curSpan.y = curSpan.x / artAspect;
        Vector2 curAspectMin = curCenter - curSpan * 0.5f;

        bestMin = curAspectMin;
        bestMax = curAspectMin + curSpan;
        bestNorth = curNorth;
        bestScore = Score(bestMin, bestMax, bestNorth, art, cap);

        Climb(freshMin, freshMin + freshSpan, 0f, NorthLimit, art, cap, ref bestMin, ref bestMax, ref bestNorth, ref bestScore);
        Climb(curAspectMin, curAspectMin + curSpan, curNorth, NorthLimit, art, cap, ref bestMin, ref bestMax, ref bestNorth, ref bestScore);
    }

    private static void Climb(Vector2 startMin, Vector2 startMax, float startNorth, float northLimit, Mask art, Mask cap,
                              ref Vector2 bestMin, ref Vector2 bestMax, ref float bestNorth, ref float bestScore)
    {
        Vector2 center = (startMin + startMax) * 0.5f;
        Vector2 span = startMax - startMin;
        float north = startNorth;
        float local = Score(center - span * 0.5f, center + span * 0.5f, north, art, cap);

        float moveStep = 0.22f;
        float degStep = 8f;

        for (int round = 0; round < 8; round++)
        {
            bool improved = false;

            for (int sx = -1; sx <= 1; sx++)
            {
                for (int sz = -1; sz <= 1; sz++)
                {
                    for (int sc = -1; sc <= 1; sc++)
                    {
                        for (int nr = -1; nr <= 1; nr++)
                        {
                            Vector2 tryCenter = center + new Vector2(sx * span.x * moveStep, sz * span.y * moveStep);
                            Vector2 trySpan = span * (1f + sc * moveStep * 0.5f);
                            float tryNorth = Mathf.Clamp(north + nr * degStep, -northLimit, northLimit);

                            float candidate = Score(tryCenter - trySpan * 0.5f, tryCenter + trySpan * 0.5f, tryNorth, art, cap);
                            if (candidate > local + 0.0004f)
                            {
                                local = candidate;
                                center = tryCenter;
                                span = trySpan;
                                north = tryNorth;
                                improved = true;
                            }
                        }
                    }
                }
            }

            moveStep *= 0.6f;
            degStep *= 0.65f;
            if (improved == false && moveStep < 0.012f) break;
        }

        if (local > bestScore)
        {
            bestScore = local;
            bestMin = center - span * 0.5f;
            bestMax = center + span * 0.5f;
            bestNorth = north;
        }
    }

    /// <summary>Every MapMarker in the loaded scene, projected through the DRAFT's rectangle (not the
    /// asset's) so the sheet shows where markers would sit if the draft were applied. Markers whose host
    /// is inactive are included and flagged - they register nothing, which matters for step F.</summary>
    private static List<MarkerInfo> CollectMarkers(Draft draft)
    {
        List<MarkerInfo> list = new List<MarkerInfo>();
        Vector2 center = (draft.worldMin + draft.worldMax) * 0.5f;
        Vector2 span = draft.worldMax - draft.worldMin;

        UnityEngine.Object[] all = Resources.FindObjectsOfTypeAll(typeof(MapMarker));
        for (int i = 0; i < all.Length; i++)
        {
            MapMarker m = all[i] as MapMarker;
            if (m == null) continue;
            if (EditorUtility.IsPersistent(m)) continue;
            if (m.gameObject.scene.IsValid() == false) continue;

            Vector3 world = m.MarkerWorldPosition;
            bool inside = TryArtUv(world, center, span, draft.north, out Vector2 uv);

            list.Add(new MarkerInfo
            {
                label = HierarchyPath(m.transform),
                world = world,
                icon = m.IconType.ToString(),
                tier = m.Tier.ToString() + (m.gameObject.activeInHierarchy ? string.Empty : " [HOST OFF]"),
                isPlayer = m.Tier == MapMarkerTier.Important,
                outside = inside == false,
                mapPoint = new Vector2((uv.x - 0.5f) * span.x, (uv.y - 0.5f) * span.y)
            });
        }

        list.Sort(delegate (MarkerInfo a, MarkerInfo b)
        {
            int byPlayer = (b.isPlayer ? 1 : 0).CompareTo(a.isPlayer ? 1 : 0);
            if (byPlayer != 0) return byPlayer;
            int byIcon = string.CompareOrdinal(a.icon, b.icon);
            if (byIcon != 0) return byIcon;
            return string.CompareOrdinal(a.label, b.label);
        });
        return list;
    }

    private static string HierarchyPath(Transform t)
    {
        string path = t.name;
        Transform parent = t.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private static void EnsureOutFolder()
    {
        if (Directory.Exists(OutFolder) == false) Directory.CreateDirectory(OutFolder);
    }

    private static string DraftPath(string sceneName)
    {
        return OutFolder + "/" + sceneName + ".draft.txt";
    }

    private static string F(float value)
    {
        return value.ToString("F4", CultureInfo.InvariantCulture);
    }

    private static void WriteDraft(string sceneName, Draft d)
    {
        string[] lines =
        {
            "# T-045 calibration draft. Edit worldMin/worldMax/north by hand, then re-run",
            "# Tools/Map/Calibration/Render Sheets From Drafts. Nothing reaches the project until",
            "# Apply Drafts To Definitions runs. Keep span.x / span.y equal to the drawing's aspect",
            "# or MapDefinition.Validate() will warn about a stretched drawing.",
            "worldMinX=" + F(d.worldMin.x),
            "worldMinZ=" + F(d.worldMin.y),
            "worldMaxX=" + F(d.worldMax.x),
            "worldMaxZ=" + F(d.worldMax.y),
            "north=" + F(d.north),
            "scoreBefore=" + F(d.scoreBefore),
            "scoreAfter=" + F(d.scoreAfter),
            "measuredMinX=" + F(d.measuredMin.x),
            "measuredMinZ=" + F(d.measuredMin.y),
            "measuredMaxX=" + F(d.measuredMax.x),
            "measuredMaxZ=" + F(d.measuredMax.y),
            "captureMinX=" + F(d.captureMin.x),
            "captureMinZ=" + F(d.captureMin.y),
            "captureMaxX=" + F(d.captureMax.x),
            "captureMaxZ=" + F(d.captureMax.y),
            "captureWidth=" + d.captureWidth.ToString(CultureInfo.InvariantCulture),
            "captureHeight=" + d.captureHeight.ToString(CultureInfo.InvariantCulture),
            "note=" + d.note
        };

        File.WriteAllLines(DraftPath(sceneName), lines);
    }

    private static bool ReadDraft(string sceneName, out Draft draft)
    {
        draft = new Draft();
        string path = DraftPath(sceneName);
        if (File.Exists(path) == false) return false;

        Dictionary<string, string> values = new Dictionary<string, string>();
        string[] lines = File.ReadAllLines(path);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            int split = line.IndexOf('=');
            if (split <= 0) continue;
            values[line.Substring(0, split)] = line.Substring(split + 1);
        }

        draft.worldMin = new Vector2(GetF(values, "worldMinX"), GetF(values, "worldMinZ"));
        draft.worldMax = new Vector2(GetF(values, "worldMaxX"), GetF(values, "worldMaxZ"));
        draft.north = GetF(values, "north");
        draft.scoreBefore = GetF(values, "scoreBefore");
        draft.scoreAfter = GetF(values, "scoreAfter");
        draft.measuredMin = new Vector2(GetF(values, "measuredMinX"), GetF(values, "measuredMinZ"));
        draft.measuredMax = new Vector2(GetF(values, "measuredMaxX"), GetF(values, "measuredMaxZ"));
        draft.captureMin = new Vector2(GetF(values, "captureMinX"), GetF(values, "captureMinZ"));
        draft.captureMax = new Vector2(GetF(values, "captureMaxX"), GetF(values, "captureMaxZ"));

        int width;
        int height;
        int.TryParse(GetRaw(values, "captureWidth"), out width);
        int.TryParse(GetRaw(values, "captureHeight"), out height);
        draft.captureWidth = width;
        draft.captureHeight = height;
        draft.note = GetRaw(values, "note");

        Vector2 span = draft.worldMax - draft.worldMin;
        if (span.x <= 0f || span.y <= 0f)
        {
            Debug.LogError($"[MapCalib] {path} holds an empty rectangle, so it cannot be rendered or applied.");
            return false;
        }

        return true;
    }

    private static string GetRaw(Dictionary<string, string> values, string key)
    {
        string value;
        return values.TryGetValue(key, out value) ? value : string.Empty;
    }

    private static float GetF(Dictionary<string, string> values, string key)
    {
        float parsed;
        return float.TryParse(GetRaw(values, key), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ? parsed : 0f;
    }

    /// <summary>Re-renders every sheet from the draft files without re-capturing. This is the iteration
    /// loop: nudge the numbers in Library/MapCalibration/&lt;scene&gt;.draft.txt, run this, read the PNG.</summary>
    [MenuItem("Tools/Map/Calibration/Render Sheets From Drafts")]
    private static void SheetsFromDrafts()
    {
        Scene current = EditorSceneManager.GetActiveScene();
        string returnPath = current.path;
        if (current.isDirty)
        {
            Debug.LogError("[MapCalib] The open scene has unsaved changes; save or revert it first, because sheets reopen scenes to read their markers.");
            return;
        }

        int rendered = 0;
        for (int i = 0; i < BuiltScenes.Length; i++)
        {
            string sceneName = BuiltScenes[i];
            Draft draft;
            if (ReadDraft(sceneName, out draft) == false) continue;

            MapDefinition def = AssetDatabase.LoadAssetAtPath<MapDefinition>(DefinitionsFolder + "/Map_" + sceneName + ".asset");
            if (def == null || def.BaseMap == null) continue;

            Scene opened = EditorSceneManager.OpenScene(ScenesFolder + sceneName + ".unity", OpenSceneMode.Single);
            if (opened.IsValid() == false) continue;

            Mask art = ReadArtMask(def.BaseMap);
            Mask cap = ReadCaptureMask(sceneName, draft);
            Mask height = BuildCaptureLayer(sceneName, draft);
            if (art == null || cap == null) continue;

            List<MarkerInfo> markers = CollectMarkers(draft);
            RenderSheet(sceneName, draft, art, cap, height, markers, string.Empty);
            WriteReport(sceneName, draft, art, cap, markers, def);
            rendered++;
        }

        if (string.IsNullOrEmpty(returnPath) == false)
            EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);

        Debug.Log($"[MapCalib] Rendered {rendered} sheet(s) from drafts. Overlap re-scored in each report.");
    }

    [MenuItem("Tools/Map/Calibration/Delete Drafts And Captures")]
    private static void DeleteWork()
    {
        if (Directory.Exists(OutFolder))
        {
            Directory.Delete(OutFolder, true);
            Debug.Log($"[MapCalib] Deleted {OutFolder}. Nothing in Assets was touched.");
        }
    }

    /// <summary>The sheet's bottom layer: the top-down capture itself, downsampled with its colours kept,
    /// so the composite shows the level as it actually looks from above - coastline, lake, mountains,
    /// clearings - instead of the axis-aligned bounds of renderers, which read as grey slabs and made a
    /// correct alignment look wrong. Water stays water on both sides, because coastlines are what the
    /// two are compared on.</summary>
    private static Mask BuildCaptureLayer(string sceneName, Draft draft)
    {
        string path = OutFolder + "/" + sceneName + "_topdown.png";
        if (File.Exists(path) == false) return null;

        Texture2D loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (ImageConversion.LoadImage(loaded, File.ReadAllBytes(path)) == false)
        {
            UnityEngine.Object.DestroyImmediate(loaded);
            return null;
        }

        int sw = loaded.width;
        int sh = loaded.height;
        Color32[] src = loaded.GetPixels32();
        UnityEngine.Object.DestroyImmediate(loaded);

        int gw = MaskGrid;
        int gh = Mathf.Max(1, Mathf.RoundToInt((float)sh * MaskGrid / sw));
        Color32[] tints = new Color32[gw * gh];
        bool[] land = new bool[gw * gh];

        for (int gy = 0; gy < gh; gy++)
        {
            int y0 = gy * sh / gh;
            int y1 = Mathf.Max(y0 + 1, (gy + 1) * sh / gh);
            for (int gx = 0; gx < gw; gx++)
            {
                int x0 = gx * sw / gw;
                int x1 = Mathf.Max(x0 + 1, (gx + 1) * sw / gw);

                int total = 0;
                long sumR = 0;
                long sumG = 0;
                long sumB = 0;
                int opaque = 0;

                for (int y = y0; y < y1; y += 2)
                    for (int x = x0; x < x1; x += 2)
                    {
                        Color32 s = src[y * sw + x];
                        if (s.a <= 24) continue;
                        total++;
                        sumR += s.r;
                        sumG += s.g;
                        sumB += s.b;
                        if (IsWater(s) == false) opaque++;
                    }

                if (total == 0) continue;

                tints[gy * gw + gx] = new Color32((byte)(sumR / total), (byte)(sumG / total), (byte)(sumB / total), 255);
                land[gy * gw + gx] = opaque * 2 > total;
            }
        }

        return new Mask { width = gw, height = gh, land = land, colour = tints, worldMin = draft.captureMin, worldMax = draft.captureMax };
    }

    /// <summary>Writes each draft into its MapDefinition. The only menu here that touches a project
    /// asset, so it re-reads every definition from disk afterwards and prints the aspect error that
    /// Validate() will see - a draft whose aspect drifted is caught here, not in play mode.</summary>
    [MenuItem("Tools/Map/Calibration/Apply Drafts To Definitions")]
    private static void ApplyDrafts()
    {
        List<string> appliedTo = new List<string>();
        for (int i = 0; i < BuiltScenes.Length; i++)
        {
            string sceneName = BuiltScenes[i];
            Draft draft;
            if (ReadDraft(sceneName, out draft) == false) continue;

            MapDefinition def = AssetDatabase.LoadAssetAtPath<MapDefinition>(DefinitionsFolder + "/Map_" + sceneName + ".asset");
            if (def == null)
            {
                Debug.LogError($"[MapCalib] There is a draft for {sceneName} but no definition to write it into.");
                continue;
            }

            Vector2 oldSpan = def.WorldMax - def.WorldMin;

            SerializedObject so = new SerializedObject(def);
            so.FindProperty("worldMin").vector2Value = draft.worldMin;
            so.FindProperty("worldMax").vector2Value = draft.worldMax;
            so.FindProperty("mapNorthDegrees").floatValue = draft.north;
            so.FindProperty("calibrated").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);

            appliedTo.Add(sceneName);
            Debug.Log($"[MapCalib] {sceneName}: rect {oldSpan.x:F0}x{oldSpan.y:F0} -> {(draft.worldMax - draft.worldMin).x:F0}x{(draft.worldMax - draft.worldMin).y:F0} m, north {draft.north:F1}.");
        }

        AssetDatabase.SaveAssets();

        int problems = 0;
        for (int i = 0; i < appliedTo.Count; i++)
        {
            MapDefinition reloaded = AssetDatabase.LoadAssetAtPath<MapDefinition>(DefinitionsFolder + "/Map_" + appliedTo[i] + ".asset");
            if (reloaded == null || reloaded.BaseMap == null)
            {
                Debug.LogError($"[MapCalib] {appliedTo[i]} no longer loads as a definition with a drawing.");
                problems++;
                continue;
            }

            Vector2 span = reloaded.WorldSpan;
            float artAspect = (float)reloaded.BaseMap.rect.width / reloaded.BaseMap.rect.height;
            float worldAspect = span.x / span.y;
            float error = Mathf.Abs(worldAspect - artAspect) / artAspect * 100f;
            if (error > 2f) problems++;

            Debug.Log($"[MapCalib] read-back {appliedTo[i]}: span {span.x:F1}x{span.y:F1} m, world aspect {worldAspect:F4} vs art {artAspect:F4} = {error:F2}% error, north {reloaded.MapNorthDegrees:F1}, Validate() = {reloaded.Validate()} problem(s).");
        }

        Debug.LogWarning(problems == 0
            ? $"[MapCalib] Applied {appliedTo.Count} draft(s), all aspect-clean. These are still draft numbers: put the player on a landmark in every scene (T-045 verification 3) before calling them calibrated."
            : $"[MapCalib] Applied {appliedTo.Count} draft(s) but {problems} disagree with their art aspect by more than 2% - fix the draft; do not ship the stretch.");
    }

    /// <summary>What a human needs to judge a draft without opening the scene: the numbers, the overlap
    /// before and after, and every marker with the same number that is printed on its dot in the sheet.</summary>
    private static void WriteReport(string sceneName, Draft draft, Mask art, Mask cap, List<MarkerInfo> markers, MapDefinition def)
    {
        Vector2 span = draft.worldMax - draft.worldMin;
        if (span.x <= 0f || span.y <= 0f) return;

        float worldAspect = span.x / span.y;
        float artAspect = def != null && def.BaseMap != null ? (float)def.BaseMap.rect.width / def.BaseMap.rect.height : 1f;
        float error = artAspect > 0f ? Mathf.Abs(worldAspect - artAspect) / artAspect * 100f : 0f;
        float score = art != null && cap != null ? Score(draft.worldMin, draft.worldMax, draft.north, art, cap) : draft.scoreAfter;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("# T-045 calibration report - " + sceneName);
        sb.AppendLine("# Sheet: " + sceneName + "_sheet.png. Background = the level itself, rendered straight down; the drawing at 52% over it.");
        sb.AppendLine("# Colour = the art drawing laid over it. Solid cyan = the draft's world rectangle; dotted");
        sb.AppendLine("# cyan = the painted island inside it. Numbered dots are markers and the number is the row");
        sb.AppendLine("# below. A red X means that marker would fall off the drawing entirely.");
        sb.AppendLine();
        sb.AppendLine("draft rect   " + V(draft.worldMin) + " -> " + V(draft.worldMax) + "   span " + span.x.ToString("F1", CultureInfo.InvariantCulture) + " x " + span.y.ToString("F1", CultureInfo.InvariantCulture) + " m");
        sb.AppendLine("north        " + draft.north.ToString("F2", CultureInfo.InvariantCulture) + " deg (the top of the drawing points this far from world +Z)");
        sb.AppendLine("aspect       world " + worldAspect.ToString("F4", CultureInfo.InvariantCulture) + " vs art " + artAspect.ToString("F4", CultureInfo.InvariantCulture) + " = " + error.ToString("F2", CultureInfo.InvariantCulture) + "% error (the gate is 2%)");
        sb.AppendLine("overlap      draft " + (score * 100f).ToString("F1", CultureInfo.InvariantCulture) + "%   as-shipped " + (draft.scoreBefore * 100f).ToString("F1", CultureInfo.InvariantCulture) + "%");
        sb.AppendLine("measured     " + V(draft.measuredMin) + " -> " + V(draft.measuredMax) + "  (every non-UI renderer, inflated by distant scenery on purpose - fit corrects it)");
        sb.AppendLine("capture      " + draft.captureWidth + " x " + draft.captureHeight + " px over " + V(draft.captureMin) + " -> " + V(draft.captureMax));
        sb.AppendLine("markers      " + markers.Count + (def != null ? "   (asset right now: span " + def.WorldSpan.x.ToString("F1", CultureInfo.InvariantCulture) + " x " + def.WorldSpan.y.ToString("F1", CultureInfo.InvariantCulture) + ", north " + def.MapNorthDegrees.ToString("F1", CultureInfo.InvariantCulture) + ")" : string.Empty));
        sb.AppendLine();
        sb.AppendLine("    n  flag    icon            tier                 world x    world z     map x    map y   sheet x  sheet y");
        for (int i = 0; i < markers.Count; i++)
        {
            MarkerInfo m = markers[i];
            string flag = m.isPlayer ? "PLAYER" : (m.outside ? "OFFMAP" : "      ");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0,4}  {1}  {2,-15}  {3,-19}  {4,8:F1}  {5,8:F1}  {6,8:F1}  {7,8:F1}  {8,7}  {9,7}",
                i + 1, flag, m.icon, m.tier, m.world.x, m.world.z, m.mapPoint.x, m.mapPoint.y,
                Mathf.RoundToInt(m.sheetPixel.x), Mathf.RoundToInt(m.sheetPixel.y)));
        }

        sb.AppendLine();
        sb.AppendLine("# paths, so you can find the host in the hierarchy:");
        for (int i = 0; i < markers.Count; i++)
            sb.AppendLine((i + 1).ToString(CultureInfo.InvariantCulture) + " = " + markers[i].label);

        File.WriteAllLines(OutFolder + "/" + sceneName + "_report.txt", sb.ToString().Split('\n'));
    }

    private static bool IsGeometryLayer(int layer)
    {
        for (int i = 0; i < GeometryLayers.Length; i++)
            if (GeometryLayers[i] == layer) return true;

        return false;
    }

    private static string V(Vector2 value)
    {
        return "(" + value.x.ToString("F1", CultureInfo.InvariantCulture) + ", " + value.y.ToString("F1", CultureInfo.InvariantCulture) + ")";
    }

    /// <summary>The image to look at: real geometry, the drawing laid over it, the draft rectangle, the
    /// painted island inside it, a metre grid and every marker numbered. Written to Library, not Assets,
    /// so a sheet costs the repo nothing and never shows up in a diff.</summary>
    private static void RenderSheet(string sceneName, Draft draft, Mask art, Mask cap, Mask height, List<MarkerInfo> markers, string suffix)
    {
        Vector2 domainMin = Vector2.Min(draft.captureMin, draft.worldMin);
        Vector2 domainMax = Vector2.Max(draft.captureMax, draft.worldMax);
        Vector2 pad = (domainMax - domainMin) * 0.06f;
        domainMin -= pad;
        domainMax += pad;

        Vector2 domainSpan = domainMax - domainMin;
        if (domainSpan.x <= 0f || domainSpan.y <= 0f) return;

        float ppm = (float)SheetMaxEdge / Mathf.Max(domainSpan.x, domainSpan.y);
        int w = Mathf.Max(64, Mathf.RoundToInt(domainSpan.x * ppm));
        int h = Mathf.Max(64, Mathf.RoundToInt(domainSpan.y * ppm));

        Color32[] pixels = new Color32[w * h];
        Color32 background = new Color32(24, 24, 30, 255);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = background;

        DrawCaptureLayer(pixels, w, h, domainMin, ppm, height);
        DrawArt(pixels, w, h, domainMin, ppm, draft, art);
        DrawGrid(pixels, w, h, domainMin, domainMax, ppm);
        DrawRects(pixels, w, h, domainMin, ppm, draft, art);
        DrawMarkers(pixels, w, h, domainMin, ppm, markers);
        DrawLegend(pixels, w, h, sceneName, draft);

        Texture2D sheet = new Texture2D(w, h, TextureFormat.RGBA32, false);
        sheet.SetPixels32(pixels);
        sheet.Apply(false, false);
        File.WriteAllBytes(OutFolder + "/" + sceneName + "_sheet" + suffix + ".png", sheet.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(sheet);
    }

    private static void DrawCaptureLayer(Color32[] pixels, int w, int h, Vector2 domainMin, float ppm, Mask height)
    {
        if (height == null || height.colour == null) return;

        float hsx = height.worldMax.x - height.worldMin.x;
        float hsy = height.worldMax.y - height.worldMin.y;
        if (hsx <= 0f || hsy <= 0f) return;

        for (int y = 0; y < h; y++)
        {
            int sy = (int)((domainMin.y + (y + 0.5f) / ppm - height.worldMin.y) / hsy * height.height);
            if ((uint)sy >= (uint)height.height) continue;

            for (int x = 0; x < w; x++)
            {
                int sx = (int)((domainMin.x + (x + 0.5f) / ppm - height.worldMin.x) / hsx * height.width);
                if ((uint)sx >= (uint)height.width) continue;

                Color32 c = height.colour[sy * height.width + sx];
                if (c.a == 0) continue;
                Blend(pixels, w, h, x, y, new Color(c.r / 255f, c.g / 255f, c.b / 255f, 1f), 0.85f);
            }
        }
    }

    private static void DrawArt(Color32[] pixels, int w, int h, Vector2 domainMin, float ppm, Draft draft, Mask art)
    {
        if (art == null || art.colour == null) return;

        Vector2 center = (draft.worldMin + draft.worldMax) * 0.5f;
        Vector2 span = draft.worldMax - draft.worldMin;
        if (span.x <= 0f || span.y <= 0f) return;

        float radians = -draft.north * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        for (int y = 0; y < h; y++)
        {
            float worldZ = domainMin.y + (y + 0.5f) / ppm;
            for (int x = 0; x < w; x++)
            {
                Vector2 rotated = Rotate(new Vector2(domainMin.x + (x + 0.5f) / ppm - center.x, worldZ - center.y), cos, sin);
                float u = 0.5f + rotated.x / span.x;
                float v = 0.5f + rotated.y / span.y;
                if (u < 0f || u > 1f || v < 0f || v > 1f) continue;

                int ax = Mathf.Min((int)(u * art.width), art.width - 1);
                int ay = Mathf.Min((int)(v * art.height), art.height - 1);
                Color32 c = art.colour[ay * art.width + ax];
                if (c.a <= 8) continue;

                // 52 % is the compromise that keeps both readable: the drawing is the subject, but the
                // grey geometry underneath is what the drawing is being checked against.
                Blend(pixels, w, h, x, y, new Color(c.r / 255f, c.g / 255f, c.b / 255f, 1f), 0.52f);
            }
        }
    }

    /// <summary>A metre grid with labels, so an offset can be read off the sheet in metres instead of
    /// guessed in pixels.</summary>
    private static void DrawGrid(Color32[] pixels, int w, int h, Vector2 domainMin, Vector2 domainMax, float ppm)
    {
        Vector2 span = domainMax - domainMin;
        float step = Mathf.Max(10f, span.x > 700f ? 100f : 50f);
        Color32 line = new Color32(255, 255, 255, 30);
        Color32 label = new Color32(215, 215, 225, 230);

        for (float gx = Mathf.CeilToInt(domainMin.x / step) * step; gx < domainMax.x; gx += step)
        {
            int px = Mathf.RoundToInt((gx - domainMin.x) * ppm);
            Line(pixels, w, h, px, 0, px, h - 1, line);
            if (Mathf.Abs(gx % (step * 2f)) < 0.01f)
                Text(pixels, w, h, px + 2, 2, ((int)gx).ToString(CultureInfo.InvariantCulture), label, 1);
        }

        for (float gz = Mathf.CeilToInt(domainMin.y / step) * step; gz < domainMax.y; gz += step)
        {
            int py = Mathf.RoundToInt((gz - domainMin.y) * ppm);
            Line(pixels, w, h, 0, py, w - 1, py, line);
            if (Mathf.Abs(gz % (step * 2f)) < 0.01f)
                Text(pixels, w, h, 2, py + 2, ((int)gz).ToString(CultureInfo.InvariantCulture), label, 1);
        }
    }

    private static void DrawRects(Color32[] pixels, int w, int h, Vector2 domainMin, float ppm, Draft draft, Mask art)
    {
        Color32 rectColour = new Color32(60, 230, 255, 255);

        Vector2Int r0 = ToSheet(draft.worldMin, domainMin, ppm);
        Vector2Int r1 = ToSheet(draft.worldMax, domainMin, ppm);
        RectOutline(pixels, w, h, r0.x, r0.y, r1.x, r1.y, rectColour);

        if (art == null) return;

        // The island is what actually depicts the level, so its own box is worth seeing inside the rect.
        ContentUv(art, out Vector2 islandMin, out Vector2 islandMax);
        Vector2 center = (draft.worldMin + draft.worldMax) * 0.5f;
        Vector2 span = draft.worldMax - draft.worldMin;

        float back = draft.north * Mathf.Deg2Rad;
        float cos = Mathf.Cos(back);
        float sin = Mathf.Sin(back);

        Vector2 a = center + Rotate(new Vector2((islandMin.x - 0.5f) * span.x, (islandMin.y - 0.5f) * span.y), cos, sin);
        Vector2 b = center + Rotate(new Vector2((islandMax.x - 0.5f) * span.x, (islandMin.y - 0.5f) * span.y), cos, sin);
        Vector2 c = center + Rotate(new Vector2((islandMax.x - 0.5f) * span.x, (islandMax.y - 0.5f) * span.y), cos, sin);
        Vector2 d = center + Rotate(new Vector2((islandMin.x - 0.5f) * span.x, (islandMax.y - 0.5f) * span.y), cos, sin);

        Vector2Int sa = ToSheet(a, domainMin, ppm);
        Vector2Int sb = ToSheet(b, domainMin, ppm);
        Vector2Int sc = ToSheet(c, domainMin, ppm);
        Vector2Int sd = ToSheet(d, domainMin, ppm);

        DottedLine(pixels, w, h, sa, sb, rectColour);
        DottedLine(pixels, w, h, sb, sc, rectColour);
        DottedLine(pixels, w, h, sc, sd, rectColour);
        DottedLine(pixels, w, h, sd, sa, rectColour);
    }

    private static void DrawMarkers(Color32[] pixels, int w, int h, Vector2 domainMin, float ppm, List<MarkerInfo> markers)
    {
        for (int i = 0; i < markers.Count; i++)
        {
            MarkerInfo m = markers[i];
            Vector2Int p = ToSheet(m.world, domainMin, ppm);
            m.sheetPixel = new Vector2(p.x, p.y);

            Disc(pixels, w, h, p.x, p.y, m.isPlayer ? 6 : 4, MarkerColour(m));
            if (m.isPlayer) Ring(pixels, w, h, p.x, p.y, 11, new Color32(255, 240, 60, 255));
            if (m.outside) Cross(pixels, w, h, p.x, p.y, 9, new Color32(255, 60, 60, 255));

            Text(pixels, w, h, p.x + 8, p.y + 5, (i + 1).ToString(CultureInfo.InvariantCulture), new Color32(255, 255, 255, 255), 2);
        }
    }

    private static void DrawLegend(Color32[] pixels, int w, int h, string sceneName, Draft draft)
    {
        Vector2 span = draft.worldMax - draft.worldMin;
        string line1 = sceneName.ToUpperInvariant() + "  DRAFT " + span.x.ToString("F0", CultureInfo.InvariantCulture) + "X" + span.y.ToString("F0", CultureInfo.InvariantCulture)
                     + "M  NORTH " + draft.north.ToString("F1", CultureInfo.InvariantCulture)
                     + "  OVERLAP " + (draft.scoreAfter * 100f).ToString("F1", CultureInfo.InvariantCulture) + "% (WAS " + (draft.scoreBefore * 100f).ToString("F1", CultureInfo.InvariantCulture) + "%)";
        string line2 = "BACKGROUND = THE LEVEL AS RENDERED FROM ABOVE   OVERLAY = DRAWING AT 52%   CYAN = DRAFT RECT   DOTTED = PAINTED ISLAND";
        string line3 = "NUMBERS = MARKERS, SEE " + sceneName.ToUpperInvariant() + "_REPORT.TXT   RED X = OFF THE DRAWING   GRID = METRES";

        Text(pixels, w, h, 6, h - 11, line1, new Color32(255, 255, 255, 255), 2);
        Text(pixels, w, h, 6, h - 23, line2, new Color32(185, 235, 255, 255), 1);
        Text(pixels, w, h, 6, h - 33, line3, new Color32(185, 235, 255, 255), 1);
    }

    /// <summary>Colour per icon type, grouped by meaning rather than by the 20 enum values: on the sheet
    /// the question is "is this a crowd, a place, or the player", not which market stall it is.</summary>
    private static Color32 MarkerColour(MarkerInfo m)
    {
        if (m.isPlayer) return new Color32(255, 220, 40, 255);

        switch (m.icon)
        {
            case "Enemy": return new Color32(255, 70, 70, 255);
            case "MonsterNpc": return new Color32(255, 150, 40, 255);
            case "HumanNpc": return new Color32(70, 220, 255, 255);
            case "MissionPoint": return new Color32(250, 120, 250, 255);
            case "Gods": return new Color32(200, 90, 255, 255);
            case "Teleport": return new Color32(255, 255, 255, 255);
            case "BossCave": return new Color32(170, 30, 30, 255);
            case "Camp": return new Color32(200, 160, 90, 255);
            case "Fire": return new Color32(255, 120, 30, 255);
            case "Cage": return new Color32(120, 160, 255, 255);
            case "Boxes": return new Color32(220, 200, 120, 255);
            default: return new Color32(120, 255, 120, 255);
        }
    }

    private static Vector2Int ToSheet(Vector3 world, Vector2 domainMin, float ppm)
    {
        return new Vector2Int(Mathf.RoundToInt((world.x - domainMin.x) * ppm), Mathf.RoundToInt((world.z - domainMin.y) * ppm));
    }

    /// <summary>Renders one sheet per cardinal orientation of the drawing, for the scene that is open,
    /// and prints the rectangle each orientation would need so the chosen one can be written into a draft.
    ///
    /// This exists because mapNorthDegrees was pinned to 0 on an inference, and the inference was wrong.
    /// A single composite cannot reveal a quarter turn: a lake that runs east-west across the middle of a
    /// level still overlaps itself after half a turn, so "the lakes line up" proved nothing. Four sheets
    /// with one overlap number each is the instrument that can.</summary>
    [MenuItem("Tools/Map/Calibration/Rotation Comparison (Current Scene)")]
    private static void RotationComparison()
    {
        Scene current = EditorSceneManager.GetActiveScene();
        if (current.isDirty)
        {
            Debug.LogError("[MapCalib] The open scene has unsaved changes; save or revert it first.");
            return;
        }

        string sceneName = current.name;
        if (System.Array.IndexOf(BuiltScenes, sceneName) < 0)
        {
            Debug.LogError($"[MapCalib] '{sceneName}' is not one of the five mapped scenes.");
            return;
        }

        EnsureOutFolder();

        MapDefinition def = AssetDatabase.LoadAssetAtPath<MapDefinition>(DefinitionsFolder + "/Map_" + sceneName + ".asset");
        if (def == null || def.BaseMap == null)
        {
            Debug.LogError($"[MapCalib] {sceneName} has no definition with a drawing.");
            return;
        }

        Draft baseDraft;
        if (ReadDraft(sceneName, out baseDraft) == false)
        {
            Debug.LogError($"[MapCalib] {sceneName}: run Capture And Fit first, so the capture exists.");
            return;
        }

        if (TryMeasureGroundFootprint(out Vector2 fpMin, out Vector2 fpMax) == false)
        {
            Debug.LogError($"[MapCalib] {sceneName}: no built-layer geometry to test against.");
            return;
        }

        Mask art = ReadArtMask(def.BaseMap);
        Mask cap = ReadCaptureMask(sceneName, baseDraft);
        Mask captureLayer = BuildCaptureLayer(sceneName, baseDraft);
        if (art == null || cap == null) return;

        float artAspect = (float)def.BaseMap.rect.width / def.BaseMap.rect.height;
        float[] angles = new float[] { 0f, 90f, 180f, 270f };

        System.Text.StringBuilder table = new System.Text.StringBuilder();
        table.AppendLine($"{sceneName} rotation comparison - footprint {(fpMax - fpMin).x:F0}x{(fpMax - fpMin).y:F0} m at centre {((fpMin + fpMax) * 0.5f).x:F0},{((fpMin + fpMax) * 0.5f).y:F0}; authored rect today {(def.WorldMax - def.WorldMin).x:F0}x{(def.WorldMax - def.WorldMin).y:F0} at north {def.MapNorthDegrees:F0}");

        for (int a = 0; a < angles.Length; a++)
        {
            RotateRect(fpMin, fpMax, angles[a], out Vector2 targetMin, out Vector2 targetMax);
            IslandFit(targetMin, targetMax, def.BaseMap, artAspect, out Vector2 wMin, out Vector2 wMax);

            Draft d = new Draft();
            d.worldMin = wMin;
            d.worldMax = wMax;
            d.north = angles[a];
            d.captureMin = baseDraft.captureMin;
            d.captureMax = baseDraft.captureMax;

            float score = Score(wMin, wMax, angles[a], art, cap);
            List<MarkerInfo> markers = CollectMarkers(d);
            int onDrawing = 0;
            for (int i = 0; i < markers.Count; i++) if (markers[i].outside == false) onDrawing++;

            RenderSheet(sceneName, d, art, cap, captureLayer, markers, "_rot" + angles[a].ToString("F0"));

            Vector2 span = wMax - wMin;
            table.AppendLine($"   north {angles[a],3:F0}  rect {span.x:F1} x {span.y:F1} m   worldMin ({wMin.x:F1}, {wMin.y:F1})  worldMax ({wMax.x:F1}, {wMax.y:F1})   overlap {score * 100f:F1}%   markers inside {onDrawing}/{markers.Count}");
        }

        Debug.Log(table.ToString());
        Debug.LogWarning($"[MapCalib] {sceneName}: four sheets written to {OutFolder}/{sceneName}_sheet_rot0|90|180|270.png. The overlap number is a tiebreaker only - look at which sheet puts the DRAWN buildings under the RENDERED ones, then put that north and its rect into {sceneName}.draft.txt and run Render Sheets From Drafts and Apply.");
    }

    /// <summary>The axis-aligned box that a world rectangle occupies once the map's own frame has been
    /// turned by -north, which is the frame IslandFit works in.</summary>
    private static void RotateRect(Vector2 rectMin, Vector2 rectMax, float north, out Vector2 outMin, out Vector2 outMax)
    {
        Vector2 centre = (rectMin + rectMax) * 0.5f;
        float radians = -north * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        outMin = new Vector2(float.MaxValue, float.MaxValue);
        outMax = new Vector2(float.MinValue, float.MinValue);

        for (int c = 0; c < 4; c++)
        {
            float x = (c % 2 == 0) ? rectMin.x : rectMax.x;
            float y = (c < 2) ? rectMin.y : rectMax.y;
            Vector2 rotated = Rotate(new Vector2(x - centre.x, y - centre.y), cos, sin);
            if (rotated.x < outMin.x) outMin.x = rotated.x;
            if (rotated.y < outMin.y) outMin.y = rotated.y;
            if (rotated.x > outMax.x) outMax.x = rotated.x;
            if (rotated.y > outMax.y) outMax.y = rotated.y;
        }

        outMin += centre;
        outMax += centre;
    }


    /// <summary>The layers that carry built, walkable geometry: Default, Wall, Floor, Mountain. Water is
    /// deliberately excluded - in Forest it is one 720 x 667 m plane whose bounds are wider than the
    /// island, so measuring it measures the sea rather than the level. Everything else (Player,
    /// EnemyLayer, Shield, Proyectile, EnemyDetection, TransparentFX, UI, MinimapIcon) is a moving thing
    /// or an editor leftover, not a landmark.</summary>
    private static readonly int[] BuiltLayers = { 0, 6, 12, 13 };

    /// <summary>What the rectangle is fitted to: the XZ footprint of the built geometry in the loaded
    /// scene. Replaced the land-overlap hill climb as the target on 11/12, because the drawings are
    /// stylised - matching their painted silhouette is not the same job as covering the area the level
    /// actually contains, and the two disagree by tens of metres.</summary>
    private static bool TryMeasureGroundFootprint(out Vector2 groundMin, out Vector2 groundMax)
    {
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        int found = 0;

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            Renderer[] renderers = roots[r].GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer ren = renderers[i];
                if (IsBuiltLayer(ren.gameObject.layer) == false) continue;
                if (ren is ParticleSystemRenderer) continue;

                // CanvasRenderer is a Renderer, and a Canvas that is not on the UI layer contributes
                // world-space bounds around the origin measured in SCREEN UNITS - which is what made
                // every scene's extents reach for z = 0 and drift between runs depending on whether the
                // HUD had drawn yet. It is not world geometry and never is.
                if (ren is CanvasRenderer) continue;

                Bounds b = ren.bounds;
                if (b.size.x > 4000f || b.size.z > 4000f) continue;

                found++;
                if (b.min.x < minX) minX = b.min.x;
                if (b.max.x > maxX) maxX = b.max.x;
                if (b.min.z < minY) minY = b.min.z;
                if (b.max.z > maxY) maxY = b.max.z;
            }
        }

        groundMin = new Vector2(minX, minY);
        groundMax = new Vector2(maxX, maxY);
        return found > 0;
    }

    private static bool IsBuiltLayer(int layer)
    {
        for (int i = 0; i < BuiltLayers.Length; i++)
            if (BuiltLayers[i] == layer) return true;

        return false;
    }

    /// <summary>The smallest rectangle with the drawing's own aspect whose painted island contains the
    /// target, anchored so the island's centre lands on the target's centre. This is the same solve
    /// MapToolbox.FitWorldRectToArtContent now performs; it is here so a draft can be produced and
    /// judged without running the RESET menu, which overwrites authored values.</summary>
    private static void IslandFit(Vector2 targetMin, Vector2 targetMax, Sprite art, float artAspect,
                                  out Vector2 worldMin, out Vector2 worldMax)
    {
        Vector2 center = (targetMin + targetMax) * 0.5f;
        Vector2 measured = targetMax - targetMin;

        Vector2 contentMin = Vector2.zero;
        Vector2 contentMax = Vector2.one;
        if (MapToolbox.TryGetArtContentRect(art, out contentMin, out contentMax) == false)
        {
            float fallbackSpanX = Mathf.Max(measured.x, artAspect * measured.y);
            Vector2 fallback = new Vector2(fallbackSpanX, fallbackSpanX / artAspect);
            worldMin = center - fallback * 0.5f;
            worldMax = worldMin + fallback;
            return;
        }

        Vector2 fraction = contentMax - contentMin;
        if (fraction.x <= 0.001f || fraction.y <= 0.001f) fraction = Vector2.one;

        Vector2 required = new Vector2(measured.x / fraction.x, measured.y / fraction.y);
        float spanX = Mathf.Max(required.x, artAspect * required.y);
        Vector2 span = new Vector2(spanX, spanX / artAspect);

        Vector2 islandCenterUv = (contentMin + contentMax) * 0.5f;
        worldMin = center - new Vector2(islandCenterUv.x * span.x, islandCenterUv.y * span.y);
        worldMax = worldMin + span;
    }


    // Pixel primitives. A sheet that needs a GUI, a font asset or a camera to draw itself is a sheet
    // that cannot run from a menu item while scenes are being swapped, so it is done by hand here.

    private static void Put(Color32[] pixels, int w, int h, int x, int y, Color32 c)
    {
        if ((uint)x >= (uint)w || (uint)y >= (uint)h) return;
        if (c.a >= 250)
        {
            pixels[y * w + x] = c;
            return;
        }

        Blend(pixels, w, h, x, y, new Color(c.r / 255f, c.g / 255f, c.b / 255f, 1f), c.a / 255f);
    }

    private static void Blend(Color32[] pixels, int w, int h, int x, int y, Color c, float alpha)
    {
        if ((uint)x >= (uint)w || (uint)y >= (uint)h) return;

        int i = y * w + x;
        Color32 d = pixels[i];
        float a = Mathf.Clamp01(alpha);

        float r = d.r / 255f + (c.r - d.r / 255f) * a;
        float g = d.g / 255f + (c.g - d.g / 255f) * a;
        float b = d.b / 255f + (c.b - d.b / 255f) * a;

        pixels[i] = new Color32(
            (byte)Mathf.RoundToInt(Mathf.Clamp01(r) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(g) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(b) * 255f),
            255);
    }

    private static void Line(Color32[] pixels, int w, int h, int x0, int y0, int x1, int y1, Color32 c)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int stepX = x0 < x1 ? 1 : -1;
        int stepY = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            Put(pixels, w, h, x0, y0, c);
            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += stepX; }
            if (e2 < dx) { err += dx; y0 += stepY; }
        }
    }

    private static void DottedLine(Color32[] pixels, int w, int h, Vector2Int a, Vector2Int b, Color32 c)
    {
        int dx = Mathf.Abs(b.x - a.x);
        int dy = Mathf.Abs(b.y - a.y);
        int steps = Mathf.Max(1, Mathf.Max(dx, dy));
        int dot = 0;

        for (int i = 0; i <= steps; i++)
        {
            dot++;
            if (dot % 4 == 0) continue;

            Put(pixels, w, h,
                Mathf.RoundToInt(Mathf.Lerp(a.x, b.x, (float)i / steps)),
                Mathf.RoundToInt(Mathf.Lerp(a.y, b.y, (float)i / steps)), c);
        }
    }

    private static void RectOutline(Color32[] pixels, int w, int h, int x0, int y0, int x1, int y1, Color32 c)
    {
        Line(pixels, w, h, x0, y0, x1, y0, c);
        Line(pixels, w, h, x1, y0, x1, y1, c);
        Line(pixels, w, h, x1, y1, x0, y1, c);
        Line(pixels, w, h, x0, y1, x0, y0, c);
    }

    private static void Disc(Color32[] pixels, int w, int h, int cx, int cy, int radius, Color32 c)
    {
        for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
                if (x * x + y * y <= radius * radius)
                    Put(pixels, w, h, cx + x, cy + y, c);
    }

    private static void Ring(Color32[] pixels, int w, int h, int cx, int cy, int radius, Color32 c)
    {
        int inner = (radius - 2) * (radius - 2);
        for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
            {
                int d = x * x + y * y;
                if (d <= radius * radius && d >= inner)
                    Put(pixels, w, h, cx + x, cy + y, c);
            }
    }

    private static void Cross(Color32[] pixels, int w, int h, int cx, int cy, int size, Color32 c)
    {
        Line(pixels, w, h, cx - size, cy - size, cx + size, cy + size, c);
        Line(pixels, w, h, cx - size, cy + size, cx + size, cy - size, c);
    }

    // A 5x7 bitmap font, top row first, so the sheet can label itself with marker numbers and metre
    // values without a TMP font, a GUI skin or a render pass. Each glyph is 35 bits packed into a
    // ulong; writing the rows as "01110..." strings keeps the shapes readable in the source.

    private static void Text(Color32[] pixels, int w, int h, int x, int y, string text, Color32 colour, int scale)
    {
        if (string.IsNullOrEmpty(text)) return;

        int pen = x;
        for (int i = 0; i < text.Length; i++)
        {
            char c = char.ToUpperInvariant(text[i]);
            ulong glyph;
            if (c != ' ' && Glyphs.TryGetValue(c, out glyph))
            {
                DrawGlyph(pixels, w, h, pen, y, glyph, new Color32(0, 0, 0, 255), scale, true);
                DrawGlyph(pixels, w, h, pen, y, glyph, colour, scale, false);
            }

            pen += 6 * scale;
        }
    }

    private static void DrawGlyph(Color32[] pixels, int w, int h, int x, int y, ulong glyph, Color32 colour, int scale, bool outlineOnly)
    {
        for (int row = 0; row < 7; row++)
        {
            int bits = (int)((glyph >> (5 * (6 - row))) & 0x1F);
            for (int col = 0; col < 5; col++)
            {
                if ((bits & (1 << (4 - col))) == 0) continue;

                for (int sy = 0; sy < scale; sy++)
                {
                    for (int sx = 0; sx < scale; sx++)
                    {
                        int px = x + col * scale + sx;
                        int py = y - row * scale - sy; // sheet y grows north, glyph rows grow down

                        if (outlineOnly)
                        {
                            Put(pixels, w, h, px - 1, py, colour);
                            Put(pixels, w, h, px + 1, py, colour);
                            Put(pixels, w, h, px, py - 1, colour);
                            Put(pixels, w, h, px, py + 1, colour);
                        }
                        else
                        {
                            Put(pixels, w, h, px, py, colour);
                        }
                    }
                }
            }
        }
    }

    private static readonly Dictionary<char, ulong> Glyphs = BuildGlyphs();

    private static Dictionary<char, ulong> BuildGlyphs()
    {
        Dictionary<char, ulong> g = new Dictionary<char, ulong>();
        g['0'] = Pack("01110100011001110101110011000101110");
        g['1'] = Pack("00100011000010000100001000010001110");
        g['2'] = Pack("01110100010000100010001000100001111");
        g['3'] = Pack("11110000010001000001000011000101110");
        g['4'] = Pack("00010001100101010010111110001000010");
        g['5'] = Pack("11111100001111000001000011000101110");
        g['6'] = Pack("00110010001000011110100011000101110");
        g['7'] = Pack("11111000010001000100010000100001000");
        g['8'] = Pack("01110100011000101110100011000101110");
        g['9'] = Pack("01110100011000101111000010001001100");
        g['A'] = Pack("01110100011000111111100011000110001");
        g['B'] = Pack("11110100011000111110100011000111110");
        g['C'] = Pack("01110100011000010000100001000101110");
        g['D'] = Pack("11100100101000110001100011001011100");
        g['E'] = Pack("11111100001000011110100001000011111");
        g['F'] = Pack("11111100001000011110100001000010000");
        g['G'] = Pack("01110100011000010111100011000101111");
        g['H'] = Pack("10001100011000111111100011000110001");
        g['I'] = Pack("01110001000010000100001000010001110");
        g['J'] = Pack("00111000100001000010000101001001100");
        g['K'] = Pack("10001100101010011000101001001010001");
        g['L'] = Pack("10000100001000010000100001000011111");
        g['M'] = Pack("10001110111010110101100011000110001");
        g['N'] = Pack("10001110011010110011100011000110001");
        g['O'] = Pack("01110100011000110001100011000101110");
        g['P'] = Pack("11110100011000111110100001000010000");
        g['Q'] = Pack("01110100011000110001101011001001101");
        g['R'] = Pack("11110100011000111110101001001010001");
        g['S'] = Pack("01111100001000001110000010000111110");
        g['T'] = Pack("11111001000010000100001000010000100");
        g['U'] = Pack("10001100011000110001100011000101110");
        g['V'] = Pack("10001100011000110001100010101000100");
        g['W'] = Pack("10001100011000110101101011101110001");
        g['X'] = Pack("10001100010101000100010101000110001");
        g['Y'] = Pack("10001100010101000100001000010000100");
        g['Z'] = Pack("11111000010001000100010001000011111");
        g['-'] = Pack("00000000000000011111000000000000000");
        g['.'] = Pack("00000000000000000000000000110001100");
        g[','] = Pack("00000000000000000000001100010001000");
        g[':'] = Pack("00000001100011000000001100011000000");
        g['/'] = Pack("00001000100001000100010000100010000");
        g['%'] = Pack("11001110100010000100010001001100110");
        g['?'] = Pack("01110100010000100110001000000000100");
        g['('] = Pack("00010001000100001000010000010000010");
        g[')'] = Pack("01000001000001000010000100010001000");
        g['='] = Pack("00000000001111100000111110000000000");
        g['['] = Pack("01110010000100001000010000100001110");
        g[']'] = Pack("01110000100001000010000100001001110");
        return g;
    }

    private static ulong Pack(string rows)
    {
        ulong value = 0;
        for (int i = 0; i < 35; i++)
        {
            value <<= 1;
            if (i < rows.Length && rows[i] == '1') value |= 1UL;
        }

        return value;
    }
}
