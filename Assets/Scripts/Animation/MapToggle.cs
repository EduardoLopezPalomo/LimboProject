using Cinemachine;
using UnityEngine;

/// <summary>
/// Opens the full-screen drawing and closes it again. The input is the <c>OpenMap</c> action
/// (M / gamepad L1, bound in StarterAssets.inputactions) delivered through
/// <see cref="StarterAssets.StarterAssetsInputs.OnOpenMapEvent"/>, so rebinding is an Inspector
/// edit and not a code change (D-11 Q7).
///
/// Two things this fixes in the version it replaces:
/// <list type="bullet">
/// <item>it pauses through <see cref="TimeEffectsManager"/> instead of writing
/// <c>Time.timeScale</c> directly, so it can never strand the game frozen or fight the parry
/// slow-motion - the house pattern used by PlayerUIController and DeathMenu;</item>
/// <item>it subscribes in Awake rather than OnEnable, because in the built scenes the GameObject
/// holding it is a child of <c>miniRoot</c>, which is exactly what opening the map switches off:
/// the old component disabled its own Update, so the big map could never be closed;</item>
/// <item>it freezes the world camera for as long as the map owns the screen
/// (<see cref="SetCameraLocked"/>), because pausing alone does not: <c>ThirdPersonController</c>
/// deliberately does not scale MOUSE look by <c>Time.deltaTime</c>, and the rig can also be moved by
/// Cinemachine itself, which no input gate can reach.</item>
/// </list>
/// </summary>
public class MapToggle : MonoBehaviour
{
    [Header("Eye animation")]
    [SerializeField] private EyeAnimation miniEye;
    [SerializeField] private EyeAnimation bigEye;

    [Header("The two framings")]
    [SerializeField] private GameObject miniRoot;
    [SerializeField] private GameObject bigRoot;

    [Header("Map")]
    [SerializeField, Tooltip("Optional. Switches the drawing between the crop and the full view under the eye animation.")]
    private MapService mapService;

    [SerializeField, Tooltip("The Player's input component. Its OnOpenMapEvent drives this, and its cursorInputForLook gate is one of the two camera locks (both live on the Player, so one reference covers both).")]
    private StarterAssets.StarterAssetsInputs playerInputs;

    [SerializeField, Tooltip("Optional. The Cinemachine brain on Main Camera, resolved at runtime when left empty. While the map is open it is switched off, which stops EVERY Cinemachine source from moving the world camera at once - orbital axis, follow damping, impulse shake. The input gate alone is not enough: see SetCameraLocked.")]
    private CinemachineBrain brain;

    private enum State { Mini, Busy, Big }

    private State state = State.Mini;
    private bool referencesValid;
    private bool lookInputLocked;
    private bool lookInputWasEnabled = true;
    private bool lookLookupAttempted;
    private bool brainLocked;
    private bool brainWasEnabled = true;
    private bool brainLookupAttempted;

    public bool IsBigMapOpen => state == State.Big;
    public bool IsBusy => state == State.Busy;

    private void Awake()
    {
        referencesValid = ValidateReferences();

        if (playerInputs != null)
            playerInputs.OnOpenMapEvent.AddListener(ToggleMap);
        else
            Debug.LogError($"{name} has no StarterAssetsInputs assigned, so nothing can open the map. Point it at the Player's input component.", this);
    }

    private void Start()
    {
        if (!referencesValid) return;

        miniRoot.SetActive(true);
        bigRoot.SetActive(false);
        miniEye.SetOpenInstant();
        state = State.Mini;

        if (mapService != null)
            mapService.ShowMinimap();
    }

    private void OnDestroy()
    {
        if (playerInputs != null)
            playerInputs.OnOpenMapEvent.RemoveListener(ToggleMap);

        SetCameraLocked(false);
        TimeEffectsManager.ResumeGame();
    }

    /// <summary>The OpenMap entry point. Ignored mid-animation so a fast double tap cannot
    /// interleave the two eye sequences.</summary>
    public void ToggleMap()
    {
        if (!referencesValid || state == State.Busy) return;

        if (state == State.Mini) OpenBigMap();
        else if (state == State.Big) CloseBigMap();
    }

    private void OpenBigMap()
    {
        state = State.Busy;
        TimeEffectsManager.PauseGame();
        SetCameraLocked(true);

        if (mapService != null)
            mapService.ShowBigMap();

        miniEye.PlayClose(() =>
        {
            miniRoot.SetActive(false);
            bigRoot.SetActive(true);
            bigEye.SetClosedInstant();
            bigEye.PlayOpen(() => state = State.Big);
        });
    }

    private void CloseBigMap()
    {
        state = State.Busy;

        if (mapService != null)
            mapService.ShowMinimap();

        bigEye.PlayClose(() =>
        {
            bigRoot.SetActive(false);
            miniRoot.SetActive(true);
            miniEye.SetClosedInstant();
            SetCameraLocked(false);
            TimeEffectsManager.ResumeGame();
            miniEye.PlayOpen(() => state = State.Mini);
        });
    }

    /// <summary>Take the camera out of the player's hands for as long as the map owns the screen, and
    /// hand it back. TWO locks, both needed, and the second one is why:
    ///
    /// <list type="bullet">
    /// <item><see cref="SetBrainLocked"/> switches the Cinemachine brain off, so nothing at all can move
    /// the world camera. This is the lock that actually stops the view.</item>
    /// <item><see cref="SetLookLocked"/> stops mouse look reaching
    /// <c>ThirdPersonController</c>, so the yaw and pitch it holds cannot drift while the map is up -
    /// without it the camera would snap the moment the brain comes back.</item>
    /// </list>
    ///
    /// Measured, not inferred: the first version shipped the input gate alone and the user could still
    /// look around, so the input path is not what moves this camera. The mover is on the Cinemachine
    /// side of the rig (Forest, Party, SpaceBoss and FirstForest each carry a scene-authored
    /// <c>CinemachineOrbitalTransposer</c>; Village does not), and a probe over a play session confirmed
    /// the camera holds with the brain off. Freezing at the brain is also the only place that covers
    /// every source at once - orbital axes, follow damping, impulse shake, blends - which is why the
    /// third-party controller is left alone.
    /// </summary>
    private void SetCameraLocked(bool locked)
    {
        SetLookLocked(locked);
        SetBrainLocked(locked);
    }

    /// <summary>Switch the Cinemachine brain off, restoring exactly the state that was found. While it
    /// is off the Camera keeps rendering from the transform it has, so the world is simply held; when
    /// it comes back the live vcam's state is re-applied, and because the world has been paused the
    /// whole time that is the pose the camera is already in, so there is nothing to glide to.</summary>
    private void SetBrainLocked(bool locked)
    {
        if (brain == null && !brainLookupAttempted)
        {
            brainLookupAttempted = true;
            brain = FindFirstObjectByType<CinemachineBrain>();

            if (brain == null)
                Debug.LogWarning($"{name} found no CinemachineBrain in the scene, so the world camera cannot be frozen while the map is open. Assign the one on Main Camera, otherwise only the input gate applies and that is not enough on the scenes whose rig orbits through Cinemachine.", this);
        }

        if (brain == null || brainLocked == locked) return;

        if (locked)
        {
            brainWasEnabled = brain.enabled;
            brain.enabled = false;
        }
        else
            brain.enabled = brainWasEnabled;

        brainLocked = locked;
    }

    /// <summary>Freeze camera orbit while the map owns the screen, then hand it back.
    ///
    /// Pausing is not enough, and the reason is one deliberate line in ThirdPersonController:
    /// <c>CameraRotation</c> scales look input by <c>Time.deltaTime</c> EXCEPT for a mouse
    /// (<c>IsCurrentDeviceMouse ? 1f : Time.deltaTime</c>), so with <c>timeScale</c> 0 a gamepad stick
    /// is frozen and a mouse is not - the world stands still and the camera still swings. And
    /// <c>LateUpdate</c> is not gated by <c>GetInputAviable</c>, so the house
    /// <c>SetInputAviable(false)</c> would not stop it either (nor should it: input is disabled during
    /// dashes, parries and attacks, where the camera must keep turning).
    ///
    /// The gate therefore lives on <see cref="StarterAssets.StarterAssetsInputs"/>:
    /// <c>cursorInputForLook</c> is exactly what <c>OnLook</c> checks before writing
    /// <c>_input.look</c>, so switching it off starves <c>CameraRotation</c>, which then keeps
    /// re-applying the yaw and pitch it already has and holds the camera exactly where it was.
    /// <c>OpenMap</c> is a separate action, so M / L1 still closes the map. Done here rather than in
    /// the controller because <c>Assets/DownloadPackages</c> is third-party and read-only in practice.
    ///
    /// <c>look</c> is zeroed on both switches: <c>StarterAssetsInputs</c> in this project never resets
    /// it (the package's <c>LateUpdate</c> reset is not in the copy in use), so a flick the instant the
    /// map opens would otherwise be applied to the camera on the frame it closes. The gate is restored
    /// to whatever it was found at instead of being forced on, so a scene authored with look off stays
    /// that way; nothing else in the project writes that field at runtime (it appears only as authored
    /// data on the Player and as prefab-instance overrides in Forest and SpaceBoss, both <c>1</c>).</summary>
    private void SetLookLocked(bool locked)
    {
        if (playerInputs == null && !lookLookupAttempted)
        {
            lookLookupAttempted = true;
            playerInputs = FindFirstObjectByType<StarterAssets.StarterAssetsInputs>();
        }

        if (playerInputs == null || lookInputLocked == locked) return;

        if (locked)
            lookInputWasEnabled = playerInputs.cursorInputForLook;
        else
            playerInputs.cursorInputForLook = lookInputWasEnabled;

        playerInputs.look = Vector2.zero;
        lookInputLocked = locked;
    }

    private bool ValidateReferences()
    {
        if (miniRoot == null || bigRoot == null || miniEye == null || bigEye == null)
        {
            Debug.LogError($"{name} is not fully wired - miniRoot, bigRoot, miniEye and bigEye are all required - so the map is disabled. In Forest and Party this is the duplicate MapManager from T-043: delete the empty one.", this);
            return false;
        }

        return true;
    }
}
