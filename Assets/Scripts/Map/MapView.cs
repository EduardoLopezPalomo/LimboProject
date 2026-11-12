using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The map you can see: one content root holding the drawing and every marker in map space,
/// plus the two framings that only ever change that root's scale and offset (D-11 Q1).
///
/// Markers are never positioned per view, so switching framings cannot make one jump. That is the
/// whole point of the design, and it is what verification item 7 in T-045 checks.
/// </summary>
public class MapView : MonoBehaviour
{
    public enum Framing
    {
        /// <summary>The masked HUD window, centred on the player.</summary>
        Minimap = 0,

        /// <summary>The full-screen drawing opened by OpenMap.</summary>
        BigMap = 1
    }

    [Header("Viewports (authored in the HUD)")]
    [SerializeField, Tooltip("EyeUI_Small/Minimap - the 200x200 Image+Mask that PlayerHUDMenu.rectTransformMinimap points at, so the HUD size and opacity settings keep working.")]
    private RectTransform minimapViewport;

    [SerializeField, Tooltip("EyeUI_BIG/Canvas - the full-screen canvas the drawing is fit into.")]
    private RectTransform bigMapViewpport;

    [Header("Behaviour")]
    [SerializeField, Tooltip("Stop the minimap crop scrolling past the edge of the drawing. Off keeps the player dead-centre, which is what the scrapped camera did.")]
    private bool clampMinimapToContent = false;

    [SerializeField, Tooltip("Upper bound on pooled marker RectTransforms per icon type.")]
    private int poolCapPerIconType = 64;

    private const string ContentName = "MapContent";
    private const string MarkerLayerName = "Markers";

    private MapDefinition definition;
    private MapIconCatalog catalog;
    private RectTransform contentRoot;
    private RectTransform markerLayer;
    private Image baseImage;
    private RectTransform placeholderLabelRect;
    private Framing framing = Framing.Minimap;

    private float framingScale = 1f;
    private float lastAppliedScale;
    private float bigMapZoom = 1f;
    private Vector2 bigMapPan;

    private readonly Dictionary<MapIconType, Queue<RectTransform>> pools = new Dictionary<MapIconType, Queue<RectTransform>>();

    /// <summary>Raised when the map-space to viewport scale changes, i.e. on a framing switch.
    /// <see cref="MapService"/> re-sizes the live markers in response; marker *positions* are
    /// untouched, which is the invariant D-11 Q1 locks.</summary>
    public event Action FramingScaleChanged;

    public bool IsBuilt => contentRoot != null;
    public RectTransform ContentRoot => contentRoot;
    public RectTransform MarkerLayer => markerLayer;
    public MapDefinition Definition => definition;
    public Framing CurrentFraming => framing;

    /// <summary>Current map-space to viewport-unit scale of the content root.</summary>
    public float FramingScale => framingScale;

    /// <summary>Size of whichever viewport is showing, in that viewport's own units.</summary>
    public Vector2 ViewportSize => ActiveViewport == null ? Vector2.zero : ActiveViewport.rect.size;

    public float BigMapZoom => bigMapZoom;

    private RectTransform ActiveViewport => framing == Framing.Minimap ? minimapViewport : bigMapViewpport;

    /// <summary>Builds the content root under the active viewport. Safe to call again: it rebuilds
    /// from scratch, which is what a scene change or a definition change needs.</summary>
    public void Build(MapDefinition newDefinition, MapIconCatalog newCatalog)
    {
        if (newDefinition == null)
        {
            Debug.LogError($"{name} cannot build a map: no MapDefinition was supplied.", this);
            return;
        }

        if (minimapViewport == null || bigMapViewpport == null)
        {
            Debug.LogError($"{name} needs both viewports assigned - EyeUI_Small/Minimap and EyeUI_BIG/Canvas.", this);
            return;
        }

        definition = newDefinition;
        catalog = newCatalog;
        Teardown();

        GameObject contentGO = new GameObject(ContentName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        contentGO.transform.SetParent(minimapViewport, false);
        contentRoot = (RectTransform)contentGO.transform;
        contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
        contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        contentRoot.pivot = new Vector2(0.5f, 0.5f);
        contentRoot.sizeDelta = definition.ContentSize;
        contentRoot.localRotation = Quaternion.identity;

        baseImage = contentGO.GetComponent<Image>();
        baseImage.sprite = definition.BaseMap;
        baseImage.type = Image.Type.Simple;
        baseImage.preserveAspect = false;
        baseImage.color = Color.white;
        baseImage.raycastTarget = false;

        GameObject markersGO = new GameObject(MarkerLayerName, typeof(RectTransform));
        markersGO.transform.SetParent(contentRoot, false);
        markerLayer = (RectTransform)markersGO.transform;
        markerLayer.anchorMin = new Vector2(0.5f, 0.5f);
        markerLayer.anchorMax = new Vector2(0.5f, 0.5f);
        markerLayer.pivot = new Vector2(0.5f, 0.5f);
        markerLayer.sizeDelta = Vector2.zero;
        markerLayer.localScale = Vector3.one;

        framing = Framing.Minimap;
        bigMapZoom = Mathf.Clamp(definition.BigMapMinZoom, definition.BigMapMinZoom, definition.BigMapMaxZoom);
        bigMapPan = Vector2.zero;

        BuildPlaceholderLabel();
        ApplyFraming(Vector3.zero);
    }

    /// <summary>A placeholder definition has no calibration, so it shows the drawing plus a label
    /// and nothing else (D-11 Q5). The label follows the active viewport rather than the content, so
    /// it stays centred and legible in the small window as well as on the big map.</summary>
    private void BuildPlaceholderLabel()
    {
        if (definition.HasCalibration || string.IsNullOrEmpty(definition.PlaceholderLabel)) return;

        GameObject labelGO = new GameObject("PlaceholderLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(minimapViewport, false);
        placeholderLabelRect = (RectTransform)labelGO.transform;
        placeholderLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
        placeholderLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
        placeholderLabelRect.pivot = new Vector2(0.5f, 0.5f);
        placeholderLabelRect.sizeDelta = new Vector2(Mathf.Min(320f, minimapViewport.rect.width), 60f);
        placeholderLabelRect.localScale = Vector3.one;

        TextMeshProUGUI text = labelGO.GetComponent<TextMeshProUGUI>();
        text.text = definition.PlaceholderLabel;
        text.fontSize = 32f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.15f, 0.13f, 0.1f, 1f);
        text.raycastTarget = false;
    }

    public void Teardown()
    {
        if (contentRoot != null)
            Destroy(contentRoot.gameObject);

        contentRoot = null;
        markerLayer = null;
        baseImage = null;
        if (placeholderLabelRect != null) Destroy(placeholderLabelRect.gameObject);
        placeholderLabelRect = null;
        pools.Clear();
    }

    /// <summary>Switches which viewport shows the content. The eye animation covers the swap, so
    /// re-parenting here is invisible and markers keep their map-space positions.</summary>
    public void SetFraming(Framing target, Vector3 playerWorld)
    {
        if (!IsBuilt) return;
        framing = target;
        ApplyFraming(playerWorld);
    }

    /// <summary>Per-frame follow of the player while in the minimap framing.</summary>
    public void FollowPlayer(Vector3 playerWorld)
    {
        if (!IsBuilt || framing != Framing.Minimap) return;
        ApplyFraming(playerWorld);
    }

    /// <summary>The one place the content root's scale and offset are written.</summary>
    public void ApplyFraming(Vector3 playerWorld)
    {
        if (!IsBuilt) return;

        RectTransform viewport = ActiveViewport;
        if (viewport == null) return;

        if (contentRoot.parent != viewport)
            contentRoot.SetParent(viewport, false);

        if (placeholderLabelRect != null && placeholderLabelRect.parent != viewport)
        {
            placeholderLabelRect.SetParent(viewport, false);
            placeholderLabelRect.anchoredPosition = Vector2.zero;
            placeholderLabelRect.localScale = Vector3.one;
        }

        Vector2 content = definition.ContentSize;
        Vector2 window = viewport.rect.size;

        if (framing == Framing.Minimap)
        {
            float cropUnits = definition.MinimapCropMetres * definition.UnitsPerMetre;
            framingScale = cropUnits > 0.0001f ? window.x / cropUnits : 1f;

            Vector2 playerMapPoint = definition.WorldToMapPoint(playerWorld);
            Vector2 offset = -playerMapPoint * framingScale;
            if (clampMinimapToContent)
                offset = ClampOffset(offset, content * framingScale, window);

            contentRoot.localScale = Vector3.one * framingScale;
            contentRoot.anchoredPosition = offset;
            baseImage.raycastTarget = false;
        }
        else
        {
            float fitX = content.x > 0.0001f ? (window.x - definition.BigMapPaddingPixels * 2f) / content.x : 1f;
            float fitY = content.y > 0.0001f ? (window.y - definition.BigMapPaddingPixels * 2f) / content.y : 1f;
            framingScale = Mathf.Max(0.0001f, Mathf.Min(fitX, fitY)) * bigMapZoom;

            contentRoot.localScale = Vector3.one * framingScale;
            contentRoot.anchoredPosition = ClampOffset(bigMapPan, content * framingScale, window);
            baseImage.raycastTarget = true;
        }

        if (!Mathf.Approximately(lastAppliedScale, framingScale))
        {
            lastAppliedScale = framingScale;
            if (FramingScaleChanged != null)
                FramingScaleChanged();
        }
    }

    /// <summary>Pan and zoom touch the framing transform only, never a marker's map-space
    /// position (D-11 Q7).</summary>
    public void AddPan(Vector2 deltaViewportUnits)
    {
        if (!IsBuilt || framing != Framing.BigMap) return;
        bigMapPan += deltaViewportUnits;
        ApplyFraming(Vector3.zero);
    }

    public void AddZoom(float multiplier)
    {
        if (!IsBuilt || framing != Framing.BigMap) return;
        bigMapZoom = Mathf.Clamp(bigMapZoom * multiplier, definition.BigMapMinZoom, definition.BigMapMaxZoom);
        ApplyFraming(Vector3.zero);
    }

    public void ResetBigMapView()
    {
        bigMapZoom = Mathf.Clamp(definition.BigMapMinZoom, definition.BigMapMinZoom, definition.BigMapMaxZoom);
        bigMapPan = Vector2.zero;
    }

    /// <summary>Takes a marker RectTransform of this icon type from the pool, creating one only when
    /// the pool is empty. Callers must never destroy what they get back - release it.</summary>
    public RectTransform AcquireMarker(MapIconType iconType)
    {
        if (!IsBuilt) return null;

        Sprite sprite = catalog == null ? null : catalog.Get(iconType);
        if (sprite == null) return null;

        if (!pools.TryGetValue(iconType, out Queue<RectTransform> pool))
        {
            pool = new Queue<RectTransform>();
            pools[iconType] = pool;
        }

        RectTransform rect = null;
        while (rect == null && pool.Count > 0)
            rect = pool.Dequeue();

        if (rect == null)
        {
            GameObject markerGO = new GameObject("Marker_" + iconType, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rect = (RectTransform)markerGO.transform;
            Image image = markerGO.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        Image target = rect.GetComponent<Image>();
        if (target != null)
        {
            target.sprite = sprite;
            target.color = Color.white;
        }

        rect.SetParent(markerLayer, false);
        rect.localRotation = Quaternion.identity;
        return rect;
    }

    public void ReleaseMarker(MapIconType iconType, RectTransform rect)
    {
        if (rect == null) return;

        rect.gameObject.SetActive(false);

        if (!pools.TryGetValue(iconType, out Queue<RectTransform> pool))
        {
            pool = new Queue<RectTransform>();
            pools[iconType] = pool;
        }

        // Bounded, so a scene that stops using an icon type hands its rectangles back instead of
        // holding them for the rest of the play session.
        if (pool.Count < poolCapPerIconType)
            pool.Enqueue(rect);
        else
            Destroy(rect.gameObject);
    }

    /// <summary>Sizes a marker in viewport units, compensating for the framing scale so the same
    /// drawing reads the same size in the minimap and on the big map.</summary>
    public void SetMarkerSize(RectTransform rect, Vector2 viewportUnits)
    {
        if (rect == null) return;
        float compensation = framingScale > 0.0001f ? 1f / framingScale : 1f;
        rect.sizeDelta = viewportUnits * compensation;
    }

    /// <summary>The marker's size in viewport units, including the quest-goal bump.</summary>
    public Vector2 GetMarkerSizeUnits(MapMarker marker)
    {
        if (definition == null || marker == null) return Vector2.zero;
        float multiplier = marker.IsGoal ? definition.GoalMarkerMultiplier : 1f;
        return definition.GetMarkerPixels(marker.Tier) * multiplier;
    }

    /// <summary>Keeps the content covering the window when it is bigger than the window, and centred
    /// when it is smaller.</summary>
    private static Vector2 ClampOffset(Vector2 desired, Vector2 contentSize, Vector2 windowSize)
    {
        float maxX = Mathf.Max(0f, (contentSize.x * 0.5f) - (windowSize.x * 0.5f));
        float maxY = Mathf.Max(0f, (contentSize.y * 0.5f) - (windowSize.y * 0.5f));
        return new Vector2(Mathf.Clamp(desired.x, -maxX, maxX), Mathf.Clamp(desired.y, -maxY, maxY));
    }
}
