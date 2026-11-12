using UnityEngine;

/// <summary>
/// Answers one question for one scene: which <see cref="MapDefinition"/> draws it. The reference is
/// serialized in the scene itself, so only the loaded scene's drawing is resident (D-11 Q6) - there
/// is deliberately no list of all five maps anywhere, and nothing here is DontDestroyOnLoad.
///
/// It can never hand back null and it can never hand back a scene with no map: a scene whose
/// definition is unset gets <see cref="fallback"/>, which draws a placeholder and shows no markers
/// because it has no calibration. That removes the "widget missing, NRE on frame 1" class entirely
/// (D-11 Q5). The one warning about it is MapService's, naming the scene once per load.
/// </summary>
public class MapSceneBinder : MonoBehaviour
{
    [SerializeField, Tooltip("This scene's map. Leave empty only for scenes that have no drawing yet.")]
    private MapDefinition definition;

    [SerializeField, Tooltip("What to show instead when this scene has no MapDefinition: the grey parchment placeholder with no calibration and no markers.")]
    private MapDefinition fallback;

    [SerializeField, Tooltip("Shared MapIconType to sprite table. Small enough to live in every scene.")]
    private MapIconCatalog iconCatalog;

    public MapIconCatalog IconCatalog => iconCatalog;
    public MapDefinition Fallback => fallback;

    /// <summary>True when the scene has its own drawing rather than the placeholder.</summary>
    public bool HasOwnDefinition => definition != null;

    /// <summary>The definition to draw, or null only if neither this scene nor the project has one.</summary>
    public MapDefinition Resolve()
    {
        if (definition != null) return definition;
        return fallback;
    }
}
