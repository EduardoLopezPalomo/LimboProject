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
/// pixel bookkeeping that otherwise drifts a marker onto the wrong landmark. Map space is
/// metres - there is no scale factor in it any more (see <see cref="ContentSize"/>).
///
/// Two groups of knobs move things and they are NOT interchangeable. Marker alignment moves the
/// PINS only and belongs to calibration. Whole map placement moves the finished map - drawing and
/// pins as one picture - is display only, and can never fix a pin that is on the wrong landmark.
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
    [SerializeField, Tooltip("Move every MARKER left/right in map units, which are world metres. +x is the drawing's right, NOT world east - this is applied after Map North Degrees, so it reads the same on the rotated maps (Village, SpaceBoss). The drawing does not move: this corrects pins that sit off their landmarks. To move the picture and the pins together use Whole Map Placement. Zero is as-calibrated.")]
    private float iconsOffsetX = 0f;

    [SerializeField, Tooltip("Move every MARKER up/down in map units, which are world metres. +y is the top of the drawing. The drawing does not move. Zero is as-calibrated.")]
    private float iconsOffsetY = 0f;

    [SerializeField, Min(0.01f), Tooltip("Multiply every marker's DISTANCE FROM THE CENTRE OF THE DRAWING. 1 is as-calibrated, 1.05 pushes markers 5% further out, 0.95 pulls them in. The drawing never scales - only the markers - so this is the knob for art drawn at a slightly different scale than the level.")]
    private float iconsRadiusMultiplier = 1f;

    [Header("Whole map placement (art + icons together)")]
    [SerializeField, Tooltip("Move the finished map - the drawing AND every icon - in map units, which are world metres. +x is the drawing's right, +y its top. Presentation only: it slides what the player SEES and never changes which world spot an icon points at, so it is NOT a calibration knob (that is Tools/Map/Calibration). It shows on the BIG MAP: the minimap crop follows the player across the placed map, so an offset cancels there and disappears. Rotation and Scale do read on both framings. Zero is as-calibrated.")]
    private Vector2 mapOffset = Vector2.zero;

    [SerializeField, Range(-180f, 180f), Tooltip("Turn the whole map about the centre of the drawing, counter-clockwise, in degrees. Drawing and icons turn as one piece and the player's arrow turns with them. This sits ON TOP of Map North Degrees and is not the same thing: Map North Degrees records which way the art was drawn (calibration - get it wrong and pins leave their landmarks), this says how the finished map should sit on screen. 0 is as-calibrated.")]
    private float mapRotationDegrees = 0f;

    [SerializeField, Min(0.05f), Tooltip("Scale the whole map - drawing, icon layout AND the icon glyphs - as one picture. 1 is as-calibrated. It magnifies, it does not re-frame: on the big map the fit is computed from the drawing at 1 (turned, never magnified), so 1.3 draws the map 30% bigger than the screen and pushes its edges out of view until the player pans, and 0.7 shrinks it inside the frame. On the minimap it works the other way round - the window is a fixed number of world metres, so a bigger map means FEWER of them visible (that is the minimap zoom knob's partner, Minimap Crop Metres).")]
    private float mapScaleMultiplier = 1f;

    [Header("Minimap framing")]
    [SerializeField, Min(1f), Tooltip("World metres across the square HUD window, at Map Scale Multiplier 1. Smaller = more zoomed in. The scrapped render-texture camera used 55 units of orthographic size for the same window, so 110 reproduces its framing. The most-asked-about feel knob.")]
    private float minimapCropMetres = 110f;

    [Header("Big map framing")]
    [SerializeField, Min(1f), Tooltip("Screen units of border kept clear around the drawing when it is FITTED to the screen. The fit is (screen minus twice this) divided by the drawing, on whichever axis is tighter, so 48 means the map never comes within 48 units of an edge. Only the fit knows about it: it does nothing once the map is zoomed in past roughly (screen / screen - 2x padding), which is also why panning is pinned to the centre until you zoom past that point.")]
    private float bigMapPaddingPixels = 48f;

    [SerializeField, Min(1f), Tooltip("How far in the player may zoom, as a multiple of the fit-to-screen scale: 4 means the drawing can reach four times the size it opens at. Nothing changes the zoom yet - no input action calls MapService.ZoomBigMap - so today this ceiling is unreachable and the map always opens at Big Map Min Zoom. It is wired for the pan+zoom feature D-11 Q7 decided, not dead code.")]
    private float bigMapMaxZoom = 4f;

    [SerializeField, Min(0.05f), Tooltip("The floor of the zoom range AND the value the big map opens at, as a multiple of the fit-to-screen scale: 1 = exactly fitted inside the screen. Raise it above 1 only if the fitted drawing is too small to read; it also raises the floor, so a player can never zoom back out below it.")]
    private float bigMapMinZoom = 1f;

    [Header("Marker size")]
    [SerializeField, Min(0.05f), Tooltip("The one knob for ALL icons: 1.15 puts every marker on the map 15% bigger, 0.8 puts them all 20% smaller. Start here rather than editing the three tier sizes below.")]
    private float markerSizeMultiplier = 1f;

    [SerializeField, Min(1f), Tooltip("Places (shops, portals, camps, quest pins): the side of the SQUARE BOX the icon is fitted into, in screen units at the canvas reference resolution - not a stretch. Art keeps its own proportions inside the box (Image.preserveAspect), which is why there is one number per tier and no width/height pair: the icons are 3:2, so a 34 box draws a 34 x 22.7 glyph. Sizes are screen units, so a pin stays legible at any crop.")]
    private float staticMarkerSize = 34f;

    [SerializeField, Min(1f), Tooltip("Characters (enemies, NPCs, monsters): the box the icon is fitted into, in screen units. Deliberately smaller than a place so a crowd reads underneath them.")]
    private float dynamicMarkerSize = 26f;

    [SerializeField, Min(1f), Tooltip("The player's own marker: the box the icon is fitted into, in screen units. The biggest of the three because it is what the player looks for. For one pin only, use MapMarker's own Size Multiplier instead of growing this.")]
    private float playerMarkerSize = 40f;

    [SerializeField, Min(0.1f), Tooltip("Applied on top of the tier size while a marker is a quest goal. Replaces the scrapped minimap icon's goal-size multiplier.")]
    private float goalMarkerMultiplier = 1.5f;

    [Header("Marker rotation")]
    [SerializeField, Tooltip("Which way the PLAYER-tier art points, measured CLOCKWISE from the top of its texture: 0 = up, 90 = right, 180 = down, 270 = left. The main_character_icon is a pair of antlers whose point and face sit at the BOTTOM of the sprite, so this is 180. Change it here if the art is redrawn facing another way - never in code.")]
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
    public float MinimapCropMetres => minimapCropMetres;
    public float CropRadiusMetres => minimapCropMetres * 0.5f;

    /// <summary>The drawing's offset inside <see cref="MapView"/>'s MapSpace node, in map units
    /// (metres). Part of Whole map placement: art and markers move together.</summary>
    public Vector2 MapOffset => mapOffset;

    /// <summary>Whole-map display rotation in degrees, counter-clockwise, about the centre of the
    /// drawing. Independent of <see cref="MapNorthDegrees"/>, which is calibration.</summary>
    public float MapRotationDegrees => mapRotationDegrees;

    /// <summary>Whole-map display scale: drawing, marker layout and marker glyphs together. Never
    /// below 0.05, because a zero here would collapse the map to a point and make the fit divide
    /// by it.</summary>
    public float MapScaleMultiplier => Mathf.Max(0.05f, mapScaleMultiplier);

    /// <summary>The crop radius expressed in the world metres that actually reach the window.
    /// Markers are pushed outward by <see cref="IconsRadiusMultiplier"/> and the whole picture is
    /// magnified by <see cref="MapScaleMultiplier"/>, so both shrink what fits: a marker at world
    /// distance d lands d x radius x scale map units from the player, and the window is
    /// <see cref="CropRadiusMetres"/> map units wide. Identical to <see cref="CropRadiusMetres"/>
    /// while both multipliers are 1, which is every uncalibrated map.
    /// <see cref="IconsOffset"/> and <see cref="MapOffset"/> cancel out - the crop follows the
    /// player, so a nudge moves the markers inside the circle, never the circle.</summary>
    public float CropRadiusMetresForMarkers => CropRadiusMetres / (IconsRadiusMultiplier * MapScaleMultiplier);
    public float BigMapPaddingPixels => bigMapPaddingPixels;
    public float BigMapMaxZoom => Mathf.Max(bigMapMinZoom, bigMapMaxZoom);
    public float BigMapMinZoom => bigMapMinZoom;
    public bool ShowCalibrationOverlay => showCalibrationOverlay;
    public float MarkerSizeMultiplier => markerSizeMultiplier;
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

    /// <summary>Size of the drawing in map units at framing scale 1 - the world rectangle itself.
    ///
    /// Map space IS metres. There used to be a Units Per Metre knob here and it was removed
    /// 11/15/2026 after measuring it: it multiplied every marker position and divided the framing
    /// scale by the same number, so on screen it cancelled exactly in both framings (and marker
    /// glyphs are sized in screen units anyway). Its one live effect was to quietly divide
    /// <see cref="IconsOffset"/>, which was added after it - a knob that did nothing except bend
    /// another one. Every asset sat at 1, so removing it changed no map.</summary>
    public Vector2 ContentSize => WorldSpan;

    /// <summary>The drawing's box AFTER Whole map placement, as an axis-aligned box: what the
    /// framing has to clamp against, because this really is the piece of UI on screen. Equal to
    /// <see cref="ContentSize"/> while Map Rotation is 0 and Map Scale is 1 - every map today - and
    /// larger once the drawing is turned or magnified.</summary>
    public Vector2 PlacedContentSize => RotatedBox(ContentSize * MapScaleMultiplier, mapRotationDegrees);

    /// <summary>The drawing's box <b>turned but not magnified</b>: what the big map FITS itself
    /// against. Rotation is in here, so turning the map keeps the whole drawing on screen; Map Scale
    /// deliberately is not, because a fit that divided by the magnification would cancel it - which
    /// is exactly what it did until 11/15/2026: the art stayed pinned to the frame while the
    /// <c>MapSpace</c> magnification still reached the pins, so Map Scale Multiplier read as an
    /// "icons only" knob. Fit against this and let the scale magnify on top: at 1 the map is fitted
    /// exactly as it always was, above 1 the drawing grows past the frame (and wants panning, once
    /// pan has an input), below 1 it shrinks inside it.</summary>
    public Vector2 FitContentSize => RotatedBox(ContentSize, mapRotationDegrees);

    /// <summary>The axis-aligned box a <paramref name="size"/> rectangle occupies once turned by
    /// <paramref name="degrees"/>: each half-extent becomes its own |cos| plus the other's |sin|.</summary>
    private static Vector2 RotatedBox(Vector2 size, float degrees)
    {
        Vector2 half = size * 0.5f;
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Abs(Mathf.Cos(radians));
        float sin = Mathf.Abs(Mathf.Sin(radians));
        return new Vector2((half.x * cos + half.y * sin) * 2f, (half.x * sin + half.y * cos) * 2f);
    }

    /// <summary>A map-space point (what <see cref="WorldToMapPoint"/> returns) moved into the space
    /// the content actually draws in, by applying Whole map placement.
    ///
    /// <see cref="MapView"/>'s MapSpace node does this to the drawing and to the pins by being their
    /// parent, so nothing that merely writes a marker position needs it. Anything measured against
    /// the VIEWPORT instead - the point the minimap crops around, the extents the pan clamp uses -
    /// has to do the same arithmetic, and doing it anywhere else is how a projection grows a second
    /// copy and drifts (see <see cref="DrawingPointToWorld"/>).</summary>
    public Vector2 ApplyMapPlacement(Vector2 mapPoint)
    {
        return Rotate(mapPoint, mapRotationDegrees) * MapScaleMultiplier + mapOffset;
    }

    /// <summary>The tier's marker box in screen units, before the global multiplier, the quest-goal
    /// bump and the per-pin multiplier on <see cref="MapMarker"/>. A box, not a stretch: the icon is
    /// fitted inside it with its own proportions. The tiers differ so a place reads as bigger than a
    /// passing citizen, and the player reads as biggest of all.</summary>
    public float GetMarkerSize(MapMarkerTier tier)
    {
        switch (tier)
        {
            case MapMarkerTier.Static: return staticMarkerSize;
            case MapMarkerTier.Player: return playerMarkerSize;
            case MapMarkerTier.Dynamic:
            default: return dynamicMarkerSize;
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
        return offset * IconsRadiusMultiplier + IconsOffset;
    }

    /// <summary>Inverse of <see cref="WorldToMapPoint"/>, for the calibration tool and the debug
    /// overlay. Undoing the alignment knobs is part of being the exact inverse, otherwise the fit
    /// tool would read back a world position from a nudged marker and re-derive the rectangle wrong.</summary>
    public Vector3 MapPointToWorld(Vector2 mapPoint, float y)
    {
        Vector2 offset = (mapPoint - IconsOffset) / IconsRadiusMultiplier;
        if (!Mathf.Approximately(mapNorthDegrees, 0f))
            offset = Rotate(offset, mapNorthDegrees);

        Vector2 center = WorldCenter;
        return new Vector3(center.x + offset.x, y, center.y + offset.y);
    }

    /// <summary>A point written in the drawing's own axes - the space <see cref="WorldMin"/> and
    /// <see cref="WorldMax"/> are authored in - as a world position. The exact inverse of
    /// <see cref="WorldOffsetMetres"/>, and the only drawing-to-world seam that does NOT carry the
    /// marker alignment knobs, because the knobs move markers and never the art.
    ///
    /// Public for the Scene-view overlay (T-047). <see cref="MapPointToWorld"/> cannot stand in for
    /// it: it un-applies the knobs, which is right for a marker and wrong for the drawing itself.
    /// Re-deriving this rotation in editor code is how a projection grows a third copy and drifts.</summary>
    public Vector3 DrawingPointToWorld(Vector2 drawingXZ, float y)
    {
        Vector2 offset = drawingXZ - WorldCenter;
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

    /// <summary>How far the world rectangle disagrees with the drawing's own aspect, as a percentage
    /// of the art's aspect: 5.5 means the art is being stretched 5.5 % to cover the level. Zero when
    /// there is nothing to compare (no drawing, no span).
    ///
    /// The single source of that number: <see cref="Validate"/> warns with it and the Scene-view
    /// overlay labels with it (T-047), so the two can never report different figures for one asset.</summary>
    public float AspectErrorPercent
    {
        get
        {
            if (baseMap == null) return 0f;
            Vector2 span = WorldSpan;
            Vector2 rect = baseMap.rect.size;
            if (span.y <= 0.0001f || rect.x <= 0f || rect.y <= 0f) return 0f;

            float worldAspect = span.x / span.y;
            float artAspect = rect.x / rect.y;
            return Mathf.Abs(worldAspect - artAspect) / artAspect * 100f;
        }
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
        float aspectError = AspectErrorPercent;
        if (aspectError > 2f)
        {
            Debug.LogWarning($"[MapDefinition] '{name}': the drawing is {artAspect:F3} aspect but the world rectangle is {worldAspect:F3}. The art stretches by {aspectError:F1}% and markers drift off its landmarks.", this);
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
