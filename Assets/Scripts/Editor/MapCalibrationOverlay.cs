using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// T-047: the Scene-view half of map calibration. <c>MapDefinition.showCalibrationOverlay</c> has
/// been an Inspector promise since Phase 1 with nothing behind it; this is the thing behind it.
///
/// It draws, for the definition the OPEN scene actually uses and nobody else, the two rectangles
/// that matter and the disagreement between them:
///
///  - the DRAWING's footprint: worldMin/worldMax turned into world space through
///    MapDefinition.DrawingPointToWorld, so the rotation is the runtime's, not a copy of it;
///  - where the MARKERS land: the same footprint scaled about the centre by
///    IconsRadiusMultiplier and shifted by IconsOffset - the alignment knobs drawn at world scale,
///    which is the only way to see what "nudge 6.2, radius 1.04" costs in metres;
///  - the top of the art (the "N" stub), which is what makes "is this map a quarter turn wrong"
///    a five-second question instead of a play-session question;
///  - the same numbers Tools/Map/Validate All Definitions prints, including the aspect error,
///    which comes from MapDefinition.AspectErrorPercent so the two can never disagree.
///
/// Deliberately scoped to the loaded scene's own definition (resolved the way MapSceneBinder
/// resolves it): five definitions drawn in every scene is noise, not a tool. Scene-view code lives
/// here and never in Assets/Scripts/Map, which stays free of UNITY_EDITOR blocks.
/// </summary>
internal static class MapCalibrationOverlay
{
    private const string MenuOverlay = "Tools/Map/Calibration Overlay/Toggle Overlay (this session)";
    private const string MenuMarkers = "Tools/Map/Calibration Overlay/Toggle Markers (this session)";

    private static bool sessionEnabled = true;
    private static bool showMarkers = true;
    private static int lastFingerprint;

    private static readonly Color ArtColor = new Color(0.25f, 1f, 0.45f, 0.9f);
    private static readonly Color ArtBadColor = new Color(1f, 0.45f, 0.2f, 0.95f);
    private static readonly Color MarkerColor = new Color(0.3f, 0.75f, 1f, 0.9f);
    private static readonly Color NorthColor = new Color(1f, 0.9f, 0.25f, 0.95f);

    [InitializeOnLoadMethod]
    private static void Install()
    {
        // Statics are reset by every domain reload, so this cannot double-subscribe - and a
        // ScriptableObject has no OnDisable to unsubscribe from, which is why the hook is here.
        SceneView.duringSceneGui += OnSceneGUI;
    }

    [MenuItem(MenuOverlay)]
    private static void ToggleOverlay()
    {
        sessionEnabled = !sessionEnabled;
        Debug.Log("[MapOverlay] overlay " + (sessionEnabled ? "on" : "off") + " for this editor session.");
        SceneView.RepaintAll();
    }

    [MenuItem(MenuOverlay, true)]
    private static void ToggleOverlayMirrorCheck()
    {
        Menu.SetChecked(MenuOverlay, sessionEnabled);
    }

    [MenuItem(MenuMarkers)]
    private static void ToggleMarkers()
    {
        showMarkers = !showMarkers;
        Debug.Log("[MapOverlay] marker dots " + (showMarkers ? "on" : "off") + ".");
        SceneView.RepaintAll();
    }

    [MenuItem(MenuMarkers, true)]
    private static void ToggleMarkersMirrorCheck()
    {
        Menu.SetChecked(MenuMarkers, showMarkers);
    }

    private static void OnSceneGUI(SceneView view)
    {
        if (sessionEnabled == false) return;

        var targets = CollectTargets();

        int fingerprint = 0;
        for (int i = 0; i < targets.Count; i++)
            fingerprint = fingerprint * 31 + targets[i].Fingerprint();

        if (fingerprint != lastFingerprint)
        {
            lastFingerprint = fingerprint;
            SceneView.RepaintAll();   // knob drags in the Inspector show up on the same frame
        }

        for (int i = 0; i < targets.Count; i++)
            DrawTarget(targets[i]);
    }

    // ------------------------------------------------------------------ collection

    private class Target
    {
        public string SceneLabel;
        public MapDefinition Definition;
        public bool UsingFallback;
        public readonly List<MapMarker> Markers = new List<MapMarker>();

        /// <summary>Everything the drawing depends on, so a knob dragged in the Inspector changes it.</summary>
        public int Fingerprint()
        {
            var d = Definition;
            if (d == null) return 0;

            int hash = d.name.GetHashCode();
            hash = hash * 31 + d.WorldMin.GetHashCode();
            hash = hash * 31 + d.WorldMax.GetHashCode();
            hash = hash * 31 + d.MapNorthDegrees.GetHashCode();
            hash = hash * 31 + d.IconsOffset.GetHashCode();
            hash = hash * 31 + d.IconsRadiusMultiplier.GetHashCode();
            hash = hash * 31 + (d.HasCalibration ? 1 : 0);
            hash = hash * 31 + d.UnitsPerMetre.GetHashCode();
            hash = hash * 31 + Markers.Count;
            hash = hash * 31 + SceneLabel.GetHashCode();
            return hash;
        }
    }

    /// <summary>One entry per loaded scene that owns a MapSceneBinder - and nothing for the other
    /// four definitions, which is the difference between a tool and a mess.</summary>
    private static List<Target> CollectTargets()
    {
        var list = new List<Target>();

        for (int s = 0; s < EditorSceneManager.sceneCount; s++)
        {
            var scene = EditorSceneManager.GetSceneAt(s);
            if (scene.IsValid() == false || scene.isLoaded == false) continue;

            var roots = scene.GetRootGameObjects();
            MapSceneBinder binder = null;
            for (int r = 0; r < roots.Length && binder == null; r++)
            {
                var found = roots[r].GetComponentsInChildren<MapSceneBinder>(true);
                if (found.Length > 0) binder = found[0];
            }
            if (binder == null) continue;

            var definition = binder.Resolve();
            if (definition == null) continue;
            if (definition.ShowCalibrationOverlay == false) continue;

            var target = new Target
            {
                SceneLabel = scene.name,
                Definition = definition,
                UsingFallback = binder.HasOwnDefinition == false
            };

            for (int r = 0; r < roots.Length; r++)
                target.Markers.AddRange(roots[r].GetComponentsInChildren<MapMarker>(true));

            list.Add(target);
        }

        return list;
    }

    // ------------------------------------------------------------------ drawing

    private static void DrawTarget(Target target)
    {
        var def = target.Definition;
        Vector2 min = def.WorldMin;
        Vector2 max = def.WorldMax;

        if (def.HasCalibration == false)
        {
            Vector3 middle = new Vector3((min.x + max.x) * 0.5f, 1f, (min.y + max.y) * 0.5f);
            Handles.Label(middle, target.SceneLabel + ": " + def.name
                          + " is a PLACEHOLDER (no calibration), so it draws no markers.",
                          EditorStyles.miniBoldLabel);
            return;
        }

        // y just above the ground so the lines do not z-fight with the terrain.
        const float y = 0.05f;

        var art = new[]
        {
            def.DrawingPointToWorld(new Vector2(min.x, min.y), y),
            def.DrawingPointToWorld(new Vector2(max.x, min.y), y),
            def.DrawingPointToWorld(new Vector2(max.x, max.y), y),
            def.DrawingPointToWorld(new Vector2(min.x, max.y), y)
        };

        float aspectError = def.AspectErrorPercent;
        Color artColor = aspectError > 2f ? ArtBadColor : ArtColor;
        Handles.color = artColor;
        Handles.DrawSolidRectangleWithOutline(art, new Color(artColor.r, artColor.g, artColor.b, 0.03f), artColor);

        // Where the markers for the drawing's own corners land. MapPointToWorld is the one seam that
        // answers "which world position projects to this map point", and ContentSize is the drawing's
        // rect in map space, so the four calls below ARE the marker footprint - no re-derived
        // scaling here. Neutral knobs put it exactly on the art rect; a radius of 1.05 shrinks this
        // one to 1/1.05, because markers pushed outward mean the world region that still fits the
        // art is smaller.
        Vector2 half = def.ContentSize * 0.5f;
        var icons = new[]
        {
            def.MapPointToWorld(new Vector2(-half.x, -half.y), y),
            def.MapPointToWorld(new Vector2(half.x, -half.y), y),
            def.MapPointToWorld(new Vector2(half.x, half.y), y),
            def.MapPointToWorld(new Vector2(-half.x, half.y), y)
        };

        Handles.color = MarkerColor;
        Handles.DrawPolyLine(icons[0], icons[1], icons[2], icons[3], icons[0]);
        Vector3 markerDrift = icons[3] - art[3];

        // Where the TOP of the drawing actually points. If this stub does not line up with the way
        // the island is drawn, mapNorthDegrees is a quarter turn out - the Forest / FirstForest /
        // Party question, answerable without a play session.
        Vector3 centre = def.DrawingPointToWorld(def.WorldCenter, y);
        Vector3 topMiddle = (art[2] + art[3]) * 0.5f;
        Handles.color = NorthColor;
        Handles.DrawLine(centre, topMiddle);
        Handles.SphereHandleCap(0, topMiddle, Quaternion.identity,
                                HandleUtility.GetHandleSize(topMiddle) * 0.045f, EventType.Repaint);

        Vector2 span = def.WorldSpan;
        string label = target.SceneLabel + "  ·  " + def.name;
        if (target.UsingFallback) label += "   (FALLBACK - this scene has no definition of its own)";
        label += "\nspan " + span.x.ToString("F0") + " x " + span.y.ToString("F0") + " m   north "
               + def.MapNorthDegrees.ToString("F0") + "°   art stretch " + aspectError.ToString("F2") + " %";
        label += "\nicons nudge (" + def.IconsOffset.x.ToString("F1") + ", " + def.IconsOffset.y.ToString("F1")
               + ") map units   radius " + def.IconsRadiusMultiplier.ToString("F3");
        label += "\nmarkers: " + target.Markers.Count + (showMarkers ? "" : " (dots hidden)")
               + "   marker corners drift (" + markerDrift.x.ToString("F1") + ", "
               + markerDrift.z.ToString("F1") + ") m";
        if (aspectError > 2f)
            label += "\nOVER 2 %: rectangle and drawing disagree - Tools/Map/Calibration measures it";

        Handles.Label(art[3] + Vector3.up * 2f, label, EditorStyles.miniBoldLabel);

        if (showMarkers) DrawMarkers(target);
    }

    private static void DrawMarkers(Target target)
    {
        for (int i = 0; i < target.Markers.Count; i++)
        {
            var marker = target.Markers[i];
            if (marker == null) continue;

            Vector3 at = marker.MarkerWorldPosition;
            bool drawn = marker.gameObject.activeInHierarchy && marker.enabled;
            float size = HandleUtility.GetHandleSize(at)
                       * (marker.Tier == MapMarkerTier.Static ? 0.05f : 0.032f);

            // Grey means "this object has a MapMarker and the map is not showing it", which is the
            // state the boss's marker is in until it is hit (D-13) and the state a disabled POI is in.
            // RectangleHandleCap, not SquareHandleCap: that one does not exist in this version.
            Handles.color = drawn ? MarkerColor : new Color(0.62f, 0.62f, 0.62f, 0.55f);
            Handles.RectangleHandleCap(0, at, Quaternion.Euler(90f, 0f, 0f), size, EventType.Repaint);
        }
    }
}

