using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The map's brain: the registry of <see cref="MapMarker"/>s, the single projection pass that turns
/// their world position into a point in map space, and the one and only place that decides whether
/// a marker is visible. Nothing else may call SetActive on a marker RectTransform - that single
/// seam is the whole cost of a future fog of war (D-11 Q4).
///
/// One per built scene, injected through the Inspector. It deliberately has no static instance
/// (Working Agreement section 4, T-023): <see cref="MapMarker"/> resolves it with
/// <see cref="Object.FindFirstObjectByType{T}"/> once, on enable.
/// </summary>
public class MapService : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField, Tooltip("The view that owns the drawing and the two framings.")]
    private MapView view;

    [SerializeField, Tooltip("Resolves this scene's MapDefinition, falling back to the placeholder so the map is never null.")]
    private MapSceneBinder binder;

    [SerializeField, Tooltip("What the minimap crops around and what dynamic markers are measured against. Found by tag when left empty.")]
    private Transform player;

    [Header("Visibility")]
    [SerializeField, Tooltip("Hide dynamic-tier markers further than this from the player. 0 is unlimited. This is the first lever to pull if a crowd scene costs too much frame time (T-045 risk list); the static and player tiers are never capped.")]
    private float dynamicMarkerMaxDistanceMetres = 0f;

    [SerializeField, Tooltip("Markers further than this from the player are not even measured for the minimap crop, just hidden. Keeps the pass arithmetic-only.")]
    private float cullBeyondExtraCropMultiplier = 1.05f;

    private readonly List<Entry> entries = new List<Entry>();
    private MapDefinition definition;
    private bool warnedNoPlayer;
    private bool warnedNoDefinition;
    private bool warnedNoView;

    /// <summary>
    /// T-058 M7: twelve paths in this class test <c>view</c> and three used to dereference it. A marker
    /// rectangle can outlive the view that drew it - the T-043 empty <c>MapManager</c> shape, a
    /// <c>MapView</c> torn down ahead of the first marker unregister at level unload - and the crash
    /// then surfaced far from the wiring that was wrong, on an enemy death or a scene change. This is
    /// the loud-once half of the answer; the callers below decide what to do without a view (hand the
    /// rectangle back to nothing, i.e. destroy it, or skip the resize), which is never a throw.
    /// </summary>
    private bool ViewMissing()
    {
        if (view != null) return false;

        if (!warnedNoView)
        {
            warnedNoView = true;
            Debug.LogError($"{name} has no MapView assigned, so its markers have nowhere to be drawn or pooled. Every built scene needs the MapManager pair: one MapService and one MapView with both viewports assigned (T-058 M7, and T-043 for the duplicate case).", this);
        }

        return true;
    }

    /// <summary>One registered marker and the RectTransform drawing it, if it has one.</summary>
    private class Entry
    {
        public MapMarker marker;
        public RectTransform rect;
        public bool rectMissingWarned;
    }

    /// <summary>How many markers are registered right now. Used by the verification checks that a
    /// killed enemy recycles its marker instead of leaking it.</summary>
    public int RegisteredCount => entries.Count;

    public MapDefinition Definition => definition;
    public MapView View => view;
    public bool HasUsableMap => view != null && view.IsBuilt && definition != null && definition.HasCalibration;

    private void Awake()
    {
        ResolveDefinition();

        if (player == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) player = playerGO.transform;
        }

        // Markers enable before or after this Awake depending on scene load order; sweep so the
        // registry is complete either way. Register is idempotent.
        MapMarker[] found = FindObjectsByType<MapMarker>(FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
            Register(found[i]);
    }

    private void LateUpdate()
    {
        if (entries.Count == 0) return;
        if (!HasUsableMap) return;

        Vector3 playerPosition = GetPlayerPosition();
        view.FollowPlayer(playerPosition);

        bool bigMap = view.CurrentFraming == MapView.Framing.BigMap;

        // The window is CropRadiusMetres of MAP space wide, but markers are pushed outward by the
        // asset's Icons Radius Multiplier, so the world metres that actually land inside it are
        // CropRadiusMetresForMarkers. Using the raw metre radius here would hide markers that are
        // comfortably on screen (multiplier below 1) or measure in ones that are off it (above 1).
        float cropRadius = definition.CropRadiusMetresForMarkers * cullBeyondExtraCropMultiplier;
        float cropRadiusSqr = cropRadius * cropRadius;

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry entry = entries[i];

            if (entry.marker == null)
            {
                RemoveAt(i);
                continue;
            }

            if (entry.rect == null) continue;

            Vector3 world = entry.marker.MarkerWorldPosition;

            // Two different switches, and both have to agree before anything draws:
            //   marker.enabled  - the authoring/code switch. A quest POI that is not currently an
            //                     objective (QuestItemInteractable / QuestZoneTrigger switch it off),
            //                     and the boss blip PhaseController holds until the fight starts.
            //   IsRevealed()    - the discovery switch (fog of war, still unwritten - D-11 Q4).
            // The enabled half belongs HERE and not only in the registration sweep below, because a
            // marker can be registered by that sweep before any code switches it off: measured in
            // SpaceBoss play mode 11/14/2026, 4 of the 7 registered entries were components with
            // enabled = false and 2 of them were on screen, so every quest objective drew from frame
            // one and D-13's hidden boss blip was never hidden. Registration order cannot fix that
            // (it depends on whether MapService.Awake or the quest component's Awake runs first);
            // consulting the flag in the one visibility seam does, for every caller, permanently.
            if (!entry.marker.enabled || !IsRevealed(entry.marker))
            {
                Hide(entry);
                continue;
            }

            if (!bigMap && entry.marker.Tier == MapMarkerTier.Dynamic && dynamicMarkerMaxDistanceMetres > 0f)
            {
                float dx = world.x - playerPosition.x;
                float dz = world.z - playerPosition.z;
                if (dx * dx + dz * dz > dynamicMarkerMaxDistanceMetres * dynamicMarkerMaxDistanceMetres)
                {
                    Hide(entry);
                    continue;
                }
            }

            Vector3 drawn = world;
            bool visible;

            if (bigMap)
            {
                visible = definition.ContainsWorld(world);
            }
            else
            {
                float dx = world.x - playerPosition.x;
                float dz = world.z - playerPosition.z;
                bool insideCrop = dx * dx + dz * dz <= cropRadiusSqr;

                if (insideCrop)
                {
                    visible = true;
                }
                else if (entry.marker.IsGoal)
                {
                    // Same marker-space radius as the visibility test above, so a goal parked on the
                    // edge lands on the drawn circle and not outside it.
                    drawn = definition.ClampToCrop(world, playerPosition, definition.CropRadiusMetresForMarkers);
                    visible = true;
                }
                else
                {
                    visible = false;
                }
            }

            if (!visible)
            {
                Hide(entry);
                continue;
            }

            Vector2 mapPoint = definition.WorldToMapPoint(drawn);
            entry.rect.anchoredPosition = mapPoint;

            if (entry.marker.Tier == MapMarkerTier.Player)
            {
                // Point the arrow the way the player faces, measured IN MAP SPACE instead of handing
                // over the raw world yaw.
                //
                // Two separate mistakes lived on this line, and the second one is why guessing at
                // art is worse than measuring it. (1) It used to be Quaternion.Euler(0, 0, yaw - north),
                // which mirrors left and right: map space is +x = world +X (east) and +y = world +Z
                // (north), a Unity yaw of theta faces (sin theta, cos theta), so facing east must draw
                // pointing right - yet +90 on a RectTransform turns its up vector to (-sin 90, cos 90)
                // = (-1, 0), i.e. left. North and south looked fine, so it read as "turns the wrong
                // way" rather than as a broken formula. (2) The angle of the facing must then be
                // corrected for which way the ART points: main_character_icon is a pair of antlers
                // whose point sits at the bottom of the texture, so it faces DOWN, and assuming up left
                // the arrow 180 degrees out at every heading.
                //
                // alpha is the facing direction in map space; a rect rotated by phi turns a texture
                // direction that sits A clockwise from up to (90 - A + phi), so solving for phi gives
                // the line below. A comes from the asset, so a redrawn icon is an Inspector edit.
                float yawRadians = entry.marker.GetYawDegrees() * Mathf.Deg2Rad;
                Vector3 ahead = drawn + new Vector3(Mathf.Sin(yawRadians), 0f, Mathf.Cos(yawRadians));
                Vector2 facing = definition.WorldToMapPoint(ahead) - mapPoint;

                if (facing.sqrMagnitude > 0.000001f)
                {
                    float alpha = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
                    entry.rect.localRotation = Quaternion.Euler(0f, 0f, alpha - 90f + definition.MarkerArtFacingDegrees);
                }
            }
            else
            {
                entry.rect.localRotation = Quaternion.identity;
            }

            if (!entry.rect.gameObject.activeSelf)
                entry.rect.gameObject.SetActive(true);
        }
    }

    /// <summary>Adds a marker to the registry. Idempotent, because a marker can enable before this
    /// component's Awake sweep reaches it.</summary>
    public void Register(MapMarker marker)
    {
        if (marker == null) return;

        for (int i = 0; i < entries.Count; i++)
            if (entries[i].marker == marker) return;

        Entry entry = new Entry { marker = marker };
        entries.Add(entry);
        AttachRect(entry);
    }

    public void Unregister(MapMarker marker)
    {
        if (marker == null) return;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].marker != marker) continue;
            RemoveAt(i);
            return;
        }
    }

    /// <summary>A marker stopped being or became a quest goal: re-size it and let the clamp kick in.</summary>
    public void OnMarkerGoalChanged(MapMarker marker)
    {
        Entry entry = Find(marker);
        if (entry == null || entry.rect == null) return;

        // T-058 M7: the goal switch is driven by quest code (QuestZoneTrigger, DialogueNPC, TeleportQuest,
        // QuestItemInteractable), which has no idea whether this scene's map is wired - and the four
        // callers run at level start, which is where an unwired view would otherwise throw.
        if (ViewMissing()) return;

        view.SetMarkerSize(entry.rect, view.GetMarkerSizeUnits(marker));
        if (marker.IsGoal)
            entry.rect.SetAsLastSibling();
    }

    /// <summary>A marker changed icon type, which means a different drawing and a different pool.</summary>
    public void OnMarkerIconChanged(MapMarker marker)
    {
        Entry entry = Find(marker);
        if (entry == null) return;

        entry.rectMissingWarned = false;
        AttachRect(entry);
    }

    /// <summary>The single visibility decision (D-11 Q4). Returns true today: there is no fog of war,
    /// and this is the only seam that would need one. Nothing outside this class may decide that a
    /// marker is visible.</summary>
    protected virtual bool IsRevealed(MapMarker marker)
    {
        return true;
    }

    /// <summary>Switches to the full-screen drawing. Framing only - no marker is re-projected.</summary>
    public void ShowBigMap()
    {
        if (view == null || !view.IsBuilt) return;
        view.ResetBigMapView();
        view.SetFraming(MapView.Framing.BigMap, GetPlayerPosition());
    }

    public void ShowMinimap()
    {
        if (view == null || !view.IsBuilt) return;
        view.SetFraming(MapView.Framing.Minimap, GetPlayerPosition());
    }

    public bool IsBigMapOpen => view != null && view.CurrentFraming == MapView.Framing.BigMap;

    public void PanBigMap(Vector2 deltaViewportUnits)
    {
        if (view != null) view.AddPan(deltaViewportUnits);
    }

    public void ZoomBigMap(float multiplier)
    {
        if (view != null) view.AddZoom(multiplier);
    }

    private void OnEnable()
    {
        if (view != null)
            view.FramingScaleChanged += OnFramingScaleChanged;
    }

    private void OnDisable()
    {
        if (view != null)
            view.FramingScaleChanged -= OnFramingScaleChanged;
    }

    private void OnFramingScaleChanged()
    {
        if (view == null) return;

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry.rect == null || entry.marker == null) continue;
            view.SetMarkerSize(entry.rect, view.GetMarkerSizeUnits(entry.marker));
        }
    }

    private void ResolveDefinition()
    {
        definition = binder != null ? binder.Resolve() : null;

        if (definition == null)
        {
            if (!warnedNoDefinition)
            {
                warnedNoDefinition = true;
                Debug.LogError($"{name} has no MapDefinition and the binder supplied no fallback, so there is no map to show. Assign Map_Fallback on the MapSceneBinder: a scene must never end up with a null map.", this);
            }
            if (view != null) view.Teardown();
            return;
        }

        if (view != null)
        {
            view.Build(definition, binder.IconCatalog);
            for (int i = 0; i < entries.Count; i++)
                AttachRect(entries[i]);
        }

        if (!definition.HasCalibration)
            Debug.LogWarning($"{name} is showing the placeholder map for scene '{gameObject.scene.name}': the drawing renders and no markers do, because there is no world-to-image calibration. Author a MapDefinition for this scene to fix it.", this);
    }

    private void AttachRect(Entry entry)
    {
        if (entry == null || entry.marker == null) return;

        if (entry.rect != null)
        {
            // T-058 M7: this release used to sit one line ABOVE the guard below it, so the naked
            // view.ReleaseMarker ran on exactly the unwired view that the next statement declares
            // unusable. Same rule as RemoveAt: no view, nothing to pool into, the rectangle goes away.
            if (view != null) view.ReleaseMarker(entry.marker.IconType, entry.rect);
            else
            {
                ViewMissing();
                Destroy(entry.rect.gameObject);
            }
            entry.rect = null;
        }

        if (view == null || !view.IsBuilt || definition == null || !definition.HasCalibration)
            return;

        entry.rect = view.AcquireMarker(entry.marker.IconType);
        if (entry.rect == null)
        {
            if (!entry.rectMissingWarned)
            {
                entry.rectMissingWarned = true;
                Debug.LogError($"{entry.marker.name} has a MapMarker of type {entry.marker.IconType} but the icon catalog has no drawing for it, so it stays invisible.", entry.marker);
            }
            return;
        }

        view.SetMarkerSize(entry.rect, view.GetMarkerSizeUnits(entry.marker));
        entry.rect.gameObject.SetActive(false);
    }

    private Entry Find(MapMarker marker)
    {
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].marker == marker) return entries[i];
        return null;
    }

    private void RemoveAt(int index)
    {
        Entry entry = entries[index];
        if (entry.rect != null)
        {
            // T-058 M7: with no view there is nothing to pool into, so the rectangle is destroyed rather
            // than asked for. This used to call view.ReleaseMarker on the strength of the rectangle alone.
            if (view != null && entry.marker != null) view.ReleaseMarker(entry.marker.IconType, entry.rect);
            else
            {
                ViewMissing();
                Destroy(entry.rect.gameObject);
            }
        }

        entries.RemoveAt(index);
    }

    private static void Hide(Entry entry)
    {
        if (entry.rect != null && entry.rect.gameObject.activeSelf)
            entry.rect.gameObject.SetActive(false);
    }

    private Vector3 GetPlayerPosition()
    {
        if (player != null) return player.position;

        if (!warnedNoPlayer)
        {
            warnedNoPlayer = true;
            Debug.LogError($"{name} has no player transform and none was found by the Player tag, so the minimap cannot follow anyone and stays on the centre of the drawing.", this);
        }

        return definition != null ? definition.MapPointToWorld(Vector2.zero, 0f) : Vector3.zero;
    }
}
