using Cinemachine;
using UnityEngine;

/// <summary>
/// Opens the full-screen drawing and closes it again. The input is the <c>OpenMap</c> action
/// (measured in StarterAssets.inputactions 11/15/2026: <c>&lt;Keyboard&gt;/m</c> and
/// <c>&lt;Gamepad&gt;/select</c> - not L1, which is Parry) delivered through
/// <see cref="StarterAssets.StarterAssetsInputs.OnOpenMapEvent"/>, so rebinding is an Inspector
/// edit and not a code change (D-11 Q7).
///
/// Two things this fixes in the version it replaces:
/// <list type="bullet">
/// <item>it pauses through <see cref="TimeEffectsManager"/> instead of writing
/// <c>Time.timeScale</c> directly, so it can never strand the game frozen or fight the parry
/// slow-motion - the house pattern used by PlayerUIController and DeathMenu;</item>
/// <item>it subscribes in Awake rather than OnEnable: the old component subscribed in OnEnable, and the
/// version it replaced disabled its own Update as the minimap closed, so the big map could never be
/// closed again. Measured 11/17/2026 while T-058 M2 was being written, this class sits on the scene root
/// <c>MapManager</c> in all five built scenes (Village read out of the live hierarchy, Forest, SpaceBoss,
/// FirstForest and Party off the scene YAML: <c>m_Father: {fileID: 0}</c>) - it is NOT under
/// <c>miniRoot</c>, and earlier notes here said it was. That is what makes the watchdog in
/// <see cref="Update"/> usable: it keeps running while the minimap is switched off and the full-screen
/// drawing owns the screen. Re-parenting this component under either root would switch that watchdog off
/// exactly when it is needed, so if that ever changes, move the watchdog with it;</item>
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

    [Header("Stuck transitions")]
    [SerializeField, Min(0.25f), Tooltip("T-058 M2: how long the open/close chain may stay mid-transition before it is declared stalled and completed by force. Measured on the authored clips (Eye_Close 0.517 s, Eye_Open 0.483 s), a full chain is about 1.0 s, so the default is roughly three times the longest single step and cannot cut a legitimate animation short. Counted in UNSCALED seconds on purpose: the map pauses the world as it opens, and a scaled clock would never advance far enough to notice anything. Raise it if a longer eye animation is ever authored.")]
    private float busyTimeoutUnscaledSeconds = 1.5f;

    private enum State { Mini, Busy, Big }

    private State state = State.Mini;
    private bool referencesValid;
    private bool lookInputLocked;
    private bool lookInputWasEnabled = true;
    private bool lookLookupAttempted;
    private bool brainLocked;
    private bool brainWasEnabled = true;
    private bool brainLookupAttempted;

    // T-058 M2: which half of the chain we are in, what it is waiting on and for how long. The chain
    // ends at an Animation Event, so every step is a place the transition can simply stop - the eye's
    // GameObject switched off under it (the eyes live under UI/HUD, so any menu or HUD fade takes them),
    // a second caller stealing the eye's single callback slot, a trigger that does not transition, an
    // Animator disabled by something else. Before this there was no record of any of it, and the only
    // symptom was a world at timeScale 0 that could not be un-mapped.
    private State stateBeforeBusy = State.Mini;
    private EyeAnimation waitingOn;
    private float busyStartedUnscaled;

    // T-058 M4: resolved on demand to NAME the screen that already owns the clock, never per frame.
    private PlayerUIController screenOwnerProbe;

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
        EnterState(State.Mini);

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

    /// <summary>The OpenMap entry point.
    ///
    /// Opening is gated (<see cref="CanOpenMap"/>), closing never is: whatever else has happened to the
    /// clock underneath the drawing, the player must always be able to hand the screen back. Mid-transition
    /// presses are ignored as before - a fast double tap must not interleave the two eye sequences - but
    /// they now say so, because "nothing happened" with no line in the console is the thing T-056 made a
    /// habit of removing. T-058 M4.
    /// </summary>
    public void ToggleMap()
    {
        if (!referencesValid)
        {
            Debug.LogWarning($"{name} was asked to toggle the map and refused: it is not wired, so the map is disabled. Awake already named the missing reference.", this);
            return;
        }

        if (state == State.Busy)
        {
            Debug.LogWarning($"{name} was asked to toggle the map mid-transition and ignored it: {(waitingOn == null ? "no eye had been armed yet" : "waiting on " + waitingOn.name + "'s " + waitingOn.PendingRequest + " event")} to close the current one (the watchdog will finish it if that never lands).", this);
            return;
        }

        if (state == State.Big)
        {
            CloseBigMap();
            return;
        }

        string blocker;
        if (!CanOpenMap(out blocker))
        {
            Debug.LogWarning($"{name} refused to open the big map: {blocker}. Close that screen first - the map pauses the world too, and whoever resumes next would hand the clock back while the drawing still owned the screen (T-058 M3/M4).", this);
            return;
        }

        OpenBigMap();
    }

    /// <summary>
    /// T-058 M4: the map is a full-screen owner of the world clock, so it may only be taken when nothing
    /// else already holds it. One test does that today, because one latch is what the game has:
    /// <see cref="TimeEffectsManager.IsPaused"/> is true for every screen that stops the world -
    /// DeathMenu, the pause, ability and options menus (PlayerUIController pauses on all four), the
    /// teleport popup - so "already paused by someone else" IS "someone else owns the screen", and
    /// opening on top of them is exactly how M3 loses a pause.
    ///
    /// What this cannot see, stated so nobody mistakes the gate for complete: a dialogue and a cutscene.
    /// Neither pauses (a dialogue only switches the action map to "UI", where OpenMap is not bound, which
    /// is why the case is rare rather than impossible) and neither exposes a signal to ask. Those two
    /// belong to D-18's owner model rather than to another FindFirstObjectByType here, and D-12 has
    /// already settled the camera half of the same question.
    /// </summary>
    private bool CanOpenMap(out string blocker)
    {
        if (TimeEffectsManager.IsPaused)
        {
            blocker = "the world is already paused by " + DescribeScreenOwner();
            return false;
        }

        blocker = null;
        return true;
    }

    /// <summary>Whoever it is, named: every screen that pauses the game today is one of the Player's four
    /// menu panels or the teleport popup, and both are reachable through public slots, so a refusal can
    /// print a name instead of a shrug. Asked on a refused key press only - never in a frame - and it
    /// returns the generic wording when it cannot work out who, which is precisely the hole D-18 exists
    /// to fill and the line that will tell us it is worth filling.</summary>
    private string DescribeScreenOwner()
    {
        if (screenOwnerProbe == null)
            screenOwnerProbe = FindFirstObjectByType<PlayerUIController>();

        if (screenOwnerProbe != null)
        {
            if (IsUp(screenOwnerProbe.DeathMenu)) return "the death menu";
            if (IsUp(screenOwnerProbe.PauseMenu)) return "the pause menu";
            if (IsUp(screenOwnerProbe.OptionsMenu)) return "the options menu";
            if (IsUp(screenOwnerProbe.AblityMenu)) return "the ability menu";
        }

        PopUpUI popup = FindFirstObjectByType<PopUpUI>();
        if (popup != null && popup.gameObject.activeInHierarchy) return "the teleport popup";

        return "another system, and pause has no owner to ask (T-058 D-18)";
    }

    private static bool IsUp(GameObject panel) => panel != null && panel.activeInHierarchy;


    private void OpenBigMap()
    {
        BeginTransition(State.Mini);
        TimeEffectsManager.PauseGame();
        SetCameraLocked(true);

        if (mapService != null)
            mapService.ShowBigMap();

        waitingOn = miniEye;
        miniEye.PlayClose(() =>
        {
            miniRoot.SetActive(false);
            bigRoot.SetActive(true);
            bigEye.SetClosedInstant();
            waitingOn = bigEye;
            bigEye.PlayOpen(() => EnterState(State.Big));
        });
    }

    private void CloseBigMap()
    {
        BeginTransition(State.Big);

        if (mapService != null)
            mapService.ShowMinimap();

        waitingOn = bigEye;
        bigEye.PlayClose(() =>
        {
            bigRoot.SetActive(false);
            miniRoot.SetActive(true);
            miniEye.SetClosedInstant();
            SetCameraLocked(false);
            TimeEffectsManager.ResumeGame();
            waitingOn = miniEye;
            miniEye.PlayOpen(() => EnterState(State.Mini));
        });
    }

    /// <summary>Enters Busy and remembers both where it came from (so a recovery knows which end is the
    /// intended one) and when it started (so the watchdog can count in unscaled seconds).</summary>
    private void BeginTransition(State from)
    {
        stateBeforeBusy = from;
        waitingOn = null;
        state = State.Busy;
        busyStartedUnscaled = Time.unscaledTime;
    }

    /// <summary>The single way out of Busy. Every step of the chain above lands here, and so does the
    /// watchdog, which is the point: there is exactly one place that says the map is at rest.</summary>
    private void EnterState(State next)
    {
        state = next;
        waitingOn = null;
    }


    /// <summary>
    /// T-058 M2: the watchdog. Every step of the chain above ends at an Animation Event, and each one is
    /// a place the transition can stop for the rest of the session - the eye's GameObject switched off
    /// under it (the eyes live under UI/HUD, so a menu, a dialogue or a HUD fade takes them), a second
    /// caller stealing the eye's single callback slot, a trigger that does not transition, an Animator
    /// switched off elsewhere. All four leave the world at timeScale 0 with the brain disabled, look input
    /// starved and M ignored - the worst shape a bug can have, because there is nothing left to press and
    /// nothing on screen explaining it. The eye animation's own tooltip already confesses the family; this
    /// covers the rest of it from the class that owns the chain.
    ///
    /// Two kinds of stall, and they get different patience. If the eye can no longer animate, its event
    /// cannot arrive and waiting is pointless, so the transition completes on this frame. Otherwise it gets
    /// the full budget, in unscaled time, so a legitimately long animation is never cut short.
    ///
    /// It COMPLETES rather than "resumes and hopes": <see cref="ApplyState"/> writes the whole end state of
    /// whichever direction was in flight, so the drawing, the two roots, the eyelid pose, the camera locks
    /// and the clock agree afterwards and the player can press M again.
    /// </summary>
    private void Update()
    {
        if (state != State.Busy) return;

        float waited = Time.unscaledTime - busyStartedUnscaled;
        bool hopeless = waitingOn != null && !waitingOn.CanAnimate;

        if (!hopeless && waited < busyTimeoutUnscaledSeconds) return;

        RecoverFromStalledTransition(waited, hopeless);
    }

    private void RecoverFromStalledTransition(float waited, bool eyeStopped)
    {
        State target = stateBeforeBusy == State.Mini ? State.Big : State.Mini;

        string step = waitingOn == null
            ? "no eye had been armed for this transition"
            : "it was waiting for " + waitingOn.name + " to finish its " + waitingOn.PendingRequest
              + (eyeStopped
                  ? " event and that Animator can no longer run (its GameObject is off or the component disabled), so the event cannot arrive"
                  : " event, which has not arrived in " + waitingOn.PendingSeconds.ToString("F2") + "s");

        Debug.LogError($"{name} has been mid-map-transition for {waited:F2}s of unscaled time (budget {busyTimeoutUnscaledSeconds:F2}s): {step}. Completing the transition to {target} so the player gets the screen, the camera and the clock back in a consistent state (T-058 M2).", this);

        // Both eyes, not only the stalled one: the step that already ran may have armed the other, and a
        // late event must not run a chain this method has finished. Cancel rather than run - ApplyState
        // below is the authority on the end state now.
        if (miniEye != null) miniEye.CancelPendingCallback();
        if (bigEye != null) bigEye.CancelPendingCallback();

        ApplyState(target);
    }

    /// <summary>What each end of the chain leaves behind, in one place, so the animation callback and the
    /// watchdog cannot disagree about what "the big map is open" means. Every statement restates a state
    /// that is normally already true when it runs, which is what makes it safe to call from either.</summary>
    private void ApplyState(State target)
    {
        bool big = target == State.Big;

        if (miniRoot != null) miniRoot.SetActive(!big);
        if (bigRoot != null) bigRoot.SetActive(big);

        if (big)
        {
            if (miniEye != null) miniEye.SetClosedInstant();
            if (bigEye != null) bigEye.SetOpenInstant();
        }
        else
        {
            if (bigEye != null) bigEye.SetClosedInstant();
            if (miniEye != null) miniEye.SetOpenInstant();
        }

        if (mapService != null)
        {
            if (big) mapService.ShowBigMap();
            else mapService.ShowMinimap();
        }

        SetCameraLocked(big);

        if (big) TimeEffectsManager.PauseGame();
        else TimeEffectsManager.ResumeGame();

        EnterState(target);
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
        {
            lookInputWasEnabled = playerInputs.cursorInputForLook;

            // The switch itself. Measured in Forest play mode 11/14/2026 this branch only remembered
            // the value and never cleared it, so cursorInputForLook stayed true with the map open and
            // the documented starvation of CameraRotation never happened: the camera held purely
            // because SetBrainLocked had switched the Cinemachine brain off. Both halves now do the
            // job they describe, and the unlock still restores whatever was found rather than true.
            playerInputs.cursorInputForLook = false;
        }
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
