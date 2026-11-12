using UnityEngine;

/// <summary>
/// One art drawing plus the calibration that turns world positions into points on it.
///
/// Authored one per built scene and referenced by that scene's <see cref="MapSceneBinder"/>,
/// so only the loaded scene's drawing is ever resident (D-11 Q6). Never gather several of
/// these into one asset, and never put one on a DontDestroyOnLoad object.
///
/// Map space is defined so that the drawing's world rectangle IS the drawing: the sprite is
/// stretched over <see cref="worldMin"/>/<see cref="worldMax"/> and markers are linear in the
/// same rectangle. That makes calibration exactly two world coordinates and removes the
/// pixel bookkeeping that otherwise drifts a marker onto the wrong landmark.
/// </summary>
[CreateAssetMenu(fileName = "MapDefinition", menuName = "Map/Map Definition")]
public class MapDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField, Tooltip("Informational. The scene holds the real reference; this names the asset and the fallback warning.")]
    private string sceneName = string.Empty;

    [Header("Art")]
    [SerializeField, Tooltip("The hand-drawn map. Stretched over the world rectangle.")]
    private Sprite baseMap;

    [Header("World -> image calibration")]
    [SerializeField, Tooltip("Off means this is a placeholder: the drawing shows, no markers do. It cannot be inferred from the rectangle, because a placeholder still needs a size to draw at.")]
    private bool calibrated = true;

    [SerializeField, Tooltip("World XZ of one corner of the drawing.")]
    private Vector2 worldMin = new Vector2(-100f, -100f);

    [SerializeField, Tooltip("World XZ of the opposite corner. +x is the drawing's right, +y is its top.")]
    private Vector2 worldMax = new Vector2(100f, 100f);

    [SerializeField, Range(-180f, 180f), Tooltip("Where the top of the drawing points, in degrees from world +Z.")]
    private float mapNorthDegrees = 0f;

    [Header("Marker alignment against the art")]
    [SerializeField, Tooltip("Move every marker LEFT/RIGHT in map-space units (metres while Units Per Metre is 1). +x is the drawing's right, NOT world east - this is applied after Map North Degrees, so it reads the same on the rotated maps (Village, SpaceBoss). Zero is as-calibrated.")]
    private float iconsOffsetX = 0f;

    [SerializeField, Tooltip("Move every marker UP/DOWN in map-space units (metres while Units Per Metre is 1). +y is the top of the drawing. Zero is as-calibrated.")]
    private float iconsOffsetY = 0f;

    [SerializeField, Min(0.01f), Tooltip("Multiply every marker's DISTANCE FROM THE CENTRE OF THE DRAWING. 1 is as-calibrated, 1.05 pushes markers 5% further out, 0.95 pulls them in. The drawing never scales - only the markers - so this is the knob for art drawn at a slightly different scale than the level.")]
    private float iconsRadiusMultiplier = 1f;

    [Header("Map space")]
    [SerializeField, Min(0.01f), Tooltip("Local units per world metre inside the map content root. 1 keeps map space in metres.")]
    private float unitsPerMetre = 1f;

    [Header("Minimap framing")]
    [SerializeField, Min(1f), Tooltip("World metres across the square HUD window. The scrapped render-texture camera used 55 units of orthographic size for the same window.")]
    private float minimapCropMetres = 110f;

    [Header("Big map framing")]
    [SerializeField, Min(1f), Tooltip("Zoom 1 fits the whole drawing inside the screen minus this much padding, in pixels.")]
    private float bigMapPaddingPixels = 48f;

    [SerializeField, Min(1f)] private float bigMapMaxZoom = 4f;

    [SerializeField, Min(0.05f)] private float bigMapMinZoom = 1f;

    [Header("Marker size in screen pixels")]
    [SerializeField, Min(1f)] private Vector2 staticMarkerPixels = new Vector2(34f, 34f);
    [SerializeField, Min(1f)] private Vector2 dynamicMarkerPixels = new Vector2(26f, 26f);
    [SerializeField, Min(1f)] private Vector2 importantMarkerPixels = new Vector2(40f, 40f);
    [SerializeField, Min(0.1f), Tooltip("Applied on top of the tier size while a marker is a quest goal. Replaces the scrapped minimap icon's goal-size multiplier.")]
    private float goalMarkerMultiplier = 1.5f;

    [Header("Marker rotation")]
    [SerializeField, Tooltip("Which way the Important-tier marker art points, measured CLOCKWISE from the top of its texture: 0 = up, 90 = right, 180 = down, 270 = left. The main_character_icon is a pair of antlers whose point and face sit at the BOTTOM of the sprite, so this is 180. Change it here if the art is redrawn facing another way.")]
    [Range(0f, 360f)] private float markerArtFacingDegrees = 180f;

    [Header("Debug")]
    [SerializeField, Tooltip("Draws the world rectangle over the drawing in the editor. Calibration drift is this system's failure mode, so keep it on while authoring.")]
    private bool showCalibrationOverlay = true;

    [Header("Placeholder only")]
    [SerializeField, Tooltip("Shown over the drawing when this definition has no calibration. Empty string means no label.")]
    private string placeholderLabel = "Map coming soon";

    /// <summary>True when a drawing and a real calibration are both present. A definition without one
    /// is a placeholder: it renders, and shows no markers (D-11 Q5).</summary>
    public bool HasCalibration => baseMap != null && calibrated && worldMax.x > worldMin.x && worldMax.y > worldMin.y;

    public bool IsCalibrated => calibrated;

    public string SceneName => sceneName;
    public Sprite BaseMap => baseMap;
    public float MapNorthDegrees => mapNorthDegrees;
    public float UnitsPerMetre => unitsPerMetre;
    public float MinimapCropMetres => minimapCropMetres;
    public float CropRadiusMetres => minimapCropMetres * 0.5f;

    /// <summary>The crop radius expressed in the world metres that actually reach the window, which
    /// is <see cref="CropRadiusMetres"/> divided by <see cref="IconsRadiusMultiplier"/>: markers are
    /// pushed outward by that multiplier, so fewer of them fit in the same HUD circle. Identical to
    /// <see cref="CropRadiusMetres"/> while the multiplier is 1, which is every uncalibrated map.
    /// <see cref="IconsOffset"/> cancels out - the crop follows the player, so the nudge cannot move
    /// the circle, only the markers inside it.</summary>
    public float CropRadiusMetresForMarkers => CropRadiusMetres / IconsRadiusMultiplier;
    public float BigMapPaddingPixels => bigMapPaddingPixels;
    public float BigMapMaxZoom => Mathf.Max(bigMapMinZoom, bigMapMaxZoom);
    public float BigMapMinZoom => bigMapMinZoom;
    public bool ShowCalibrationOverlay => showCalibrationOverlay;
    public float GoalMarkerMultiplier => goalMarkerMultiplier;
    public float MarkerArtFacingDegrees => markerArtFacingDegrees;
    public string PlaceholderLabel => placeholderLabel;
    public Vector2 WorldMin => worldMin;
    public Vector2 WorldMax => worldMax;

    /// <summary>The authoring nudge applied to every marker, in map-space units: +x is the drawing's
    /// right, +y its top. Read-only here; authored under Marker alignment against the art.</summary>
    public Vector2 IconsOffset => new Vector2(iconsOffsetX, iconsOffsetY);

    /// <summary>Never below 0.01 even if the asset somehow holds 0: a zero here would collapse every
    /// marker onto one point and make <see cref="MapPointToWorld"/> divide by zero.</summary>
    public float IconsRadiusMultiplier => Mathf.Max(0.01f, iconsRadiusMultiplier);

    /// <summary>Size of the world rectangle, in metres.</summary>
    public Vector2 WorldSpan => new Vector2(Mathf.Abs(worldMax.x - worldMin.x), Mathf.Abs(worldMax.y - worldMin.y));

    /// <summary>World XZ at the centre of the drawing.</summary>
    public Vector2 WorldCenter => (worldMin + worldMax) * 0.5f;

    /// <summary>Size of the map content root in map-space units at framing scale 1.</summary>
    public Vector2 ContentSize => WorldSpan * unitsPerMetre;

    /// <summary>The tier's marker size in viewport units, before the quest-goal bump. Tier 1 and
    /// tier 2 differ so a place reads as bigger than a passing citizen.</summary>
    public Vector2 GetMarkerPixels(MapMarkerTier tier)
    {
        switch (tier)
        {
            case MapMarkerTier.Static: return staticMarkerPixels;
            case MapMarkerTier.Important: return importantMarkerPixels;
            case MapMarkerTier.Dynamic:
            default: return dynamicMarkerPixels;
        }
    }

    /// <summary>World XZ to a point in map space: origin at the centre of the drawing, +y up.
    ///
    /// The one world-to-map seam in the game - markers, the player arrow's facing and the minimap
    /// crop centre all come through here, so the three can never disagree. The two Marker alignment
    /// knobs therefore belong here and nowhere else: scale the distance from the drawing's centre,
    /// then translate. Order matters - the nudge is added LAST so it is in map axes (the drawing's
    /// left/right) rather than world axes, and it is not itself scaled.</summary>
    public Vector2 WorldToMapPoint(Vector3 world)
    {
        Vector2 offset = WorldOffsetMetres(world);
        return offset * (unitsPerMetre * IconsRadiusMultiplier) + IconsOffset;
    }

    /// <summary>Inverse of <see cref="WorldToMapPoint"/>, for the calibration tool and the debug
    /// overlay. Undoing the alignment knobs is part of being the exact inverse, otherwise the fit
    /// tool would read back a world position from a nudged marker and re-derive the rectangle wrong.</summary>
    public Vector3 MapPointToWorld(Vector2 mapPoint, float y)
    {
        Vector2 offset = (mapPoint - IconsOffset) / (unitsPerMetre * IconsRadiusMultiplier);
        if (!Mathf.Approximately(mapNorthDegrees, 0f))
            offset = Rotate(offset, mapNorthDegrees);

        Vector2 center = WorldCenter;
        return new Vector3(center.x + offset.x, y, center.y + offset.y);
    }

    /// <summary>True when a world position falls inside the drawing's world rectangle.
    ///
    /// Deliberately NOT affected by the alignment knobs: those move the markers, never the art, so
    /// "is this thing on the drawing at all" is still the raw rectangle. A marker nudged off the edge
    /// is a tuning artefact to fix in the Inspector, not something to hide here.</summary>
    public bool ContainsWorld(Vector3 world)
    {
        Vector2 offset = WorldOffsetMetres(world);
        Vector2 half = WorldSpan * 0.5f;
        return Mathf.Abs(offset.x) <= half.x && Mathf.Abs(offset.y) <= half.y;
    }

    /// <summary>World XZ relative to the centre of the drawing, in metres, already turned into the
    /// drawing's own axes (so +x is its right, +y its top) but with no marker alignment applied.</summary>
    private Vector2 WorldOffsetMetres(Vector3 world)
    {
        Vector2 center = WorldCenter;
        Vector2 offset = new Vector2(world.x - center.x, world.z - center.y);
        if (!Mathf.Approximately(mapNorthDegrees, 0f))
            offset = Rotate(offset, -mapNorthDegrees);

        return offset;
    }

    /// <summary>Clamps a world position to the crop circle around <paramref name="center"/> so a quest
    /// goal sits on the minimap edge instead of vanishing off it. Same job as the old
    /// MinimapCamera.ClampToEdge, done in world space so markers stay in map space.</summary>
    public Vector3 ClampToCrop(Vector3 world, Vector3 center, float radiusMetres)
    {
        float dx = world.x - center.x;
        float dz = world.z - center.z;
        float distanceSqr = dx * dx + dz * dz;
        if (distanceSqr <= radiusMetres * radiusMetres || distanceSqr <= 0.0001f)
            return world;

        float scale = radiusMetres / Mathf.Sqrt(distanceSqr);
        return new Vector3(center.x + dx * scale, world.y, center.z + dz * scale);
    }

    /// <summary>Advisory checks used by the calibration tool. Never blocks loading a scene.</summary>
    public int Validate()
    {
        int problemCount = 0;

        if (baseMap == null)
        {
            Debug.LogError($"[MapDefinition] '{name}' has no base map, so it can only serve as the placeholder.", this);
            return problemCount + 1;
        }

        if (calibrated == false)
            return 0;

        if (worldMax.x <= worldMin.x || worldMax.y <= worldMin.y)
        {
            Debug.LogError($"[MapDefinition] '{name}' is marked calibrated but its world rectangle is empty (worldMin {worldMin}, worldMax {worldMax}).", this);
            return problemCount + 1;
        }

        Vector2 span = WorldSpan;
        Vector2 rect = baseMap.rect.size;
        float worldAspect = span.x / span.y;
        float artAspect = rect.x / rect.y;
        if (Mathf.Abs(worldAspect - artAspect) / artAspect > 0.02f)
        {
            Debug.LogWarning($"[MapDefinition] '{name}': the drawing is {artAspect:F3} aspect but the world rectangle is {worldAspect:F3}. The art stretches by {Mathf.Abs(worldAspect - artAspect) / artAspect * 100f:F1}% and markers drift off its landmarks.", this);
            problemCount++;
        }

        // Advisory only, deliberately NOT counted as a problem: a map with tuned markers is a working
        // map. But a big nudge or a radius far from 1 is a calibration error wearing a disguise - the
        // rectangle says the level is one size and the markers need another to land on the art. Warn
        // so the real fix (Tools/Map/Calibration) gets made, and so a tuned value is never mistaken
        // for a measurement. Thresholds are relative to the drawing: 5% of a 253 m level is not 5% of
        // a 1074 m one.
        float nudgeWarnAt = Mathf.Max(span.x, span.y) * 0.05f;
        if (Mathf.Abs(iconsOffsetX) > nudgeWarnAt || Mathf.Abs(iconsOffsetY) > nudgeWarnAt)
            Debug.LogWarning($"[MapDefinition] '{name}': markers are nudged by ({iconsOffsetX:F1}, {iconsOffsetY:F1}) map units, more than 5% of the drawing. Something is off between the rectangle and the art - measure it with Tools/Map/Calibration rather than parking it in Icons Offset.", this);

        float radiusWarnAt = Mathf.Abs(IconsRadiusMultiplier - 1f);
        if (radiusWarnAt > 0.05f)
            Debug.LogWarning($"[MapDefinition] '{name}': markers sit at {IconsRadiusMultiplier:F2}x their calibrated distance from the centre, {radiusWarnAt * 100f:F1}% off. That is a scale error between the world rectangle and the drawing; the fit tool measures it, and Icons Radius Multiplier should go back to 1 once it is right.", this);

        return problemCount;
    }

    private static Vector2 Rotate(Vector2 value, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
    }
}
