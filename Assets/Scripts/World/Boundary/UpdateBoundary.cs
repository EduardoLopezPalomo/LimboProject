using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Feeds the player's position to the boundary shader (<c>_PlayerLocation</c>) so the invisible outer walls
/// fade in as the player walks them.
///
/// This used to call <c>SetVector</c> straight on the shared material asset, which cost two things (T-044):
/// <list type="bullet">
/// <item>in the Editor, writing to a loaded asset marks it dirty — so simply PLAYING the game rewrote
/// <c>Assets/Materials/BoundaryMaterial.mat</c> and the player's last position landed in the next commit;</item>
/// <item>a value that every boundary renderer reads identically does not belong to the asset anyway: Unity's
/// tool for "this uniform, per renderer" is <see cref="MaterialPropertyBlock"/>, which lives in memory only.</item>
/// </list>
///
/// The renderers are collected at <see cref="Start"/> rather than referenced, because this component sits on
/// <c>External_walls/BoundaryManager</c>, which has no Renderer of its own — Forest's 23 wall renderers are
/// its siblings, not its children. So they are found by asking every loaded scene which renderers draw
/// <see cref="BoundaryMaterial"/>. Every scene that places a boundary is covered without a single Inspector
/// assignment, and nothing on disk is written.
/// </summary>
public class UpdateBoundary : MonoBehaviour
{
    private static readonly int PlayerLocation = Shader.PropertyToID("_PlayerLocation");

    public Transform player;
    public Material BoundaryMaterial;

    private Renderer[] boundaryRenderers;
    private MaterialPropertyBlock properties;
    private bool warnedMissingPlayer;

    // Awake is too early to find the walls: measured in Forest on 11/15/2026, the scan below returned 0
    // renderers from Awake and 23 from Start onwards, because GetRootGameObjects() does not return the rest of
    // the scene while it is still being deserialized. Start is the first safe point; the retry in Update covers
    // a boundary that only exists a few frames later, and the cap stops a scene that genuinely has no boundary
    // from scanning every frame forever.
    private const int MaxResolveAttempts = 10;
    private int resolveAttempts;

    void Awake()
    {
        if (BoundaryMaterial == null)
        {
            Debug.LogError($"{name}: BoundaryMaterial is not assigned, so the boundary effect has no material to " +
                "receive the player position. Disabling this component.", this);
            enabled = false;
            return;
        }

        properties = new MaterialPropertyBlock();
    }

    void Start()
    {
        if (properties != null)
        {
            CollectBoundaryRenderers();
        }
    }

    void Update()
    {
        if (properties == null) return;

        if (player == null)
        {
            if (!warnedMissingPlayer)
            {
                warnedMissingPlayer = true;
                Debug.LogError($"{name}: 'player' is not assigned, so the boundary effect stays frozen at its last " +
                    "position. Further warnings from this component are suppressed.", this);
            }
            return;
        }

        if (boundaryRenderers == null && resolveAttempts < MaxResolveAttempts)
        {
            CollectBoundaryRenderers();
        }

        if (boundaryRenderers == null) return;

        properties.SetVector(PlayerLocation, player.position);

        for (int i = 0; i < boundaryRenderers.Length; i++)
        {
            if (boundaryRenderers[i] != null)
            {
                boundaryRenderers[i].SetPropertyBlock(properties);
            }
        }
    }

    private void CollectBoundaryRenderers()
    {
        resolveAttempts++;
        var found = new System.Collections.Generic.List<Renderer>();

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded) continue;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Renderer[] candidates = roots[rootIndex].GetComponentsInChildren<Renderer>(true);
                for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
                {
                    if (UsesBoundaryMaterial(candidates[candidateIndex]))
                    {
                        found.Add(candidates[candidateIndex]);
                    }
                }
            }
        }

        if (found.Count == 0)
        {
            if (resolveAttempts >= MaxResolveAttempts)
            {
                Debug.LogError($"{name}: no Renderer in any loaded scene draws '{BoundaryMaterial.name}' after " +
                    $"{resolveAttempts} attempts, so the boundary effect has nothing to display. Check that this " +
                    "scene still contains the boundary walls that use that material.", this);
            }
            return;
        }

        boundaryRenderers = found.ToArray();
    }

    private bool UsesBoundaryMaterial(Renderer renderer)
    {
        Material[] materials = renderer.sharedMaterials;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == BoundaryMaterial) return true;
        }

        return false;
    }
}

