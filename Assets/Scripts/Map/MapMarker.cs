using UnityEngine;

/// <summary>
/// Puts this GameObject on the map. Replaces the scrapped <c>MinimapIcon</c>, which built a
/// world-space Quad rendered by a second orthographic camera; this only declares a tier and an
/// icon, and <see cref="MapService"/> draws it into the UI marker layer (T-045, D-11 Q3).
///
/// A marker follows its host's active state and its <c>enabled</c> flag, so the existing quest
/// code that does <c>GetComponent&lt;MapMarker&gt;().enabled = isObjective</c> keeps working: Unity
/// calls OnDisable for both the GameObject and the behaviour being turned off, and
/// <see cref="MapService"/> consults <c>enabled</c> in its visibility pass as well, so a marker that
/// starts the scene switched off never draws - the flag is the whole switch, in either direction.
/// </summary>
public class MapMarker : MonoBehaviour
{
    [Header("What this marker is")]
    [SerializeField, Tooltip("Static = a place. Dynamic = anything that walks (position only). Player = the player, and the only tier allowed to turn with its yaw.")]
    private MapMarkerTier tier = MapMarkerTier.Dynamic;

    [SerializeField, Tooltip("Which drawing from Assets/UI Images/Map/Elements to use, resolved through MapIconCatalog.")]
    private MapIconType iconType = MapIconType.HumanNpc;

    [Header("Placement")]
    [SerializeField, Tooltip("Offset from the host's transform, in world units. Useful when the trigger volume is not where the landmark appears to be.")]
    private Vector3 worldOffset = Vector3.zero;

    [Header("Size")]
    [SerializeField, Min(0.05f), Tooltip("This pin's OWN multiplier on top of its tier's size on the MapDefinition asset: 1 = the tier size, 1.5 = half as big again. Use it for the one pin that has to stand out (a boss, a unique landmark) instead of growing the whole tier. An active quest goal gets the definition's Goal Marker Multiplier on top of this.")]
    private float sizeMultiplier = 1f;

    private MapService service;
    private bool isRegistered;
    private bool isGoal;
    private bool warnedMissingService;

    public MapMarkerTier Tier => tier;
    public MapIconType IconType => iconType;

    /// <summary>This pin's own size multiplier, applied by <see cref="MapView.GetMarkerSizeUnits"/>
    /// after the tier size and before the quest-goal bump. The tier size and the definition's global
    /// Marker Size Multiplier live on the asset; this is the per-pin exception.</summary>
    public float SizeMultiplier => sizeMultiplier;

    /// <summary>True while a quest goal points at this marker. Drives the size bump and the
    /// minimap-edge clamp, replacing the old goal colour swap.</summary>
    public bool IsGoal => isGoal;

    /// <summary>Where the marker sits, in world space.</summary>
    public Vector3 MarkerWorldPosition => transform.position + worldOffset;

    /// <summary>Horizontal rotation used by the <see cref="MapMarkerTier.Player"/> tier, which is
    /// the only tier allowed to turn. MapService converts the yaw into map space; if the asset turns
    /// the whole map with Map Rotation Degrees, the arrow turns with it, because it lives under the
    /// same MapSpace node as the drawing.</summary>
    public float GetYawDegrees() => transform.eulerAngles.y;

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        // Deliberately not gated on isRegistered: MapService's Awake sweep registers markers that never
        // saw their own OnEnable, so isRegistered is still false here while the entry is live. Gating on
        // it kept such a marker in the registry - icon and all - after the game switched it off.
        // Unregister is a no-op when this marker is not registered, so the only test that matters is
        // whether we know which service to ask.
        if (service == null) return;

        service.Unregister(this);
        isRegistered = false;
    }

    /// <summary>The quest-side switch the four migrated call sites use
    /// (QuestItemInteractable, QuestZoneTrigger, DialogueNPC, TeleportQuest). Signature kept from
    /// MinimapIcon.SetAsGoal so the migration is a type swap, not a rewrite.</summary>
    public void SetAsGoal(bool goal)
    {
        if (isGoal == goal) return;

        isGoal = goal;
        if (isRegistered && service != null)
            service.OnMarkerGoalChanged(this);
    }

    /// <summary>Lets code change which drawing is used, e.g. a quest that means "any of these".
    /// Prefer authoring the right type on the prefab.</summary>
    public void SetIconType(MapIconType newType)
    {
        if (iconType == newType) return;

        iconType = newType;
        if (isRegistered && service != null)
            service.OnMarkerIconChanged(this);
    }

    private void Register()
    {
        if (isRegistered) return;

        if (service == null)
            service = FindFirstObjectByType<MapService>();

        if (service == null)
        {
            if (!warnedMissingService)
            {
                warnedMissingService = true;
                Debug.LogWarning($"{name} has a MapMarker but the scene has no MapService, so it cannot be drawn. Every built scene needs a MapManager holding one.", this);
            }
            return;
        }

        service.Register(this);
        isRegistered = true;
    }
}
