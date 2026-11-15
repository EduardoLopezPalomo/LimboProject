using System;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;

/// <summary>
/// The ONE door first-party code goes through to freeze the player's movement.
///
/// WHY THIS CLASS EXISTS (T-001, P0)
/// The lock itself lives in third-party Starter Assets: the StarterAssets
/// <c>ThirdPersonController</c> exposes a setter whose name is misspelled
/// (<c>SetInputAviable</c>) that flips a single public bool, and its <c>Update()</c> returns early
/// while that bool is false. The bool has no owner, no timer and no reset, so every caller had to
/// pair its own false/true by hand - and 13 call sites in 7 files did exactly that. Any interrupt
/// between the pair (death, knockback, scene change, god-space teleport, a cancelled ability, an
/// AnimationClip cut short before the event that restores it) killed the coroutine that owned the
/// second half, and the player came back unable to move. Worst player-facing bug in the combat layer.
///
/// HOW IT IS FIXED
///  1. Locks are NAMED and SET-BASED - not a bool, not a counter. Each caller states who it is (its
///     <c>AbilityIds</c> name, or one of <see cref="LockOwner"/>), and movement stays frozen until
///     every holder has released. Idempotent in both directions, which matters because several of
///     these calls are AnimationEvent handlers, and a clip that gets cut short can fire an event
///     twice or never. A counter would be strictly worse here: acquire-twice / release-once leaves
///     the player frozen forever, which is the very bug being fixed.
///  2. <see cref="OnDisable"/> drops every lock, mirroring what <see cref="MakeInvincible"/> does for
///     i-frames (decided in D-06): a lock can never outlive the character that took it, so death,
///     respawn, level unload and teleport all self-heal.
///  3. <see cref="Update"/> reconciles a FOREIGN write. The StarterAssets controller also flips its
///     own bool from <c>InputAfterDeath()</c>, which <c>PlayerDeath.anim</c> fires as an AnimationEvent
///     straight at that component; the whole DownloadPackages tree is read-only, so that write cannot
///     be routed through here. Left alone it would re-create the bug: nobody ever releases the
///     "Death" holder, so the next unrelated release would freeze movement permanently. So if the bool
///     says "free" while we still believe we hold locks, the bool wins: the holders are dropped and it
///     is logged once per life.
///
/// WHAT THIS DELIBERATELY DOES NOT CHANGE
/// <see cref="EnableInput"/> and <see cref="DisableInput"/> keep their names AND their blunt effect.
/// They are AnimationEvent targets - <c>HitShield.anim</c> calls DisableInput and then EnableInput -
/// so renaming them would silently break those clips, and a guard hit has always restored movement
/// for everybody. They now go through the same bookkeeping so the two channels cannot disagree.
/// The PlayerInput half (<see cref="PlayerInput.ActivateInput"/> / <c>DeactivateInput</c>) is the
/// whole-input channel menus and level loads use, and stays separate from the movement lock.
///
/// HOUSE RULE (reported by AbilityReferenceValidator, which counts it as a problem):
/// the third-party setter is called from THIS FILE and nowhere else. Need the player to stop moving?
/// AcquireInputLock with your own holder, and release it on every path out of your ability.
/// </summary>
public class InputController : MonoBehaviour
{
    /// <summary>
    /// Holders that are not abilities. An ability never uses this: it passes its own
    /// <see cref="AbilityIds"/> name, so the holder of a lock reads exactly like the ability that
    /// took it and two different abilities can never overwrite each other's lock.
    /// </summary>
    public static class LockOwner
    {
        /// <summary>Raised on death. PlayerDeath.anim's InputAfterDeath event drops it (see Update).</summary>
        public const string Death = "Death";

        /// <summary>Raised by the whole-input channel (menus, cutscenes) via DisableInput.</summary>
        public const string UiOrCutscene = "UiOrCutscene";
    }

    /// <summary>
    /// Reference to the ThirdPersonController component on this GameObject.
    /// Used to control input availability.
    /// </summary>
    private ThirdPersonController controller;
    
    /// <summary>
    /// Reference to the PlayerInput component from StarterAssets.
    /// Handles the actual input system activation.
    /// </summary>
    public PlayerInput playerInput;
    
    /// <summary>Who is holding the movement lock right now. Empty means the player can move.</summary>
    private readonly HashSet<string> holders = new HashSet<string>(StringComparer.Ordinal);

    private bool missingControllerReported;
    private bool adoptedForeignUnlock;

    /// <summary>True while at least one holder has the movement frozen.</summary>
    public bool IsMovementLocked { get { return holders.Count > 0; } }

    /// <summary>True if this specific holder currently has the movement frozen.</summary>
    public bool HoldsInputLock(string holder)
    {
        return holders.Contains(holder);
    }

    private void Awake()
    {
        // Find the ThirdPersonController component attached to this object
        controller = GetComponent<ThirdPersonController>();
    }

    /// <summary>
    /// Publishes whatever is still held instead of clearing again: a lock taken while this object was
    /// inactive (a level-load path that freezes the player before switching them on) has to survive the
    /// enable, or the flag would stay false with nobody holding it - the exact leak this class kills.
    /// </summary>
    private void OnEnable()
    {
        adoptedForeignUnlock = false;
        WriteFlag(holders.Count == 0);
    }

    /// <summary>
    /// THE SAFETY NET. Disabling the character (death, respawn, level unload, god-space teleport) ends
    /// every movement lock they were holding and hands movement back, so a lock can never outlive the
    /// state change that took it. Same reasoning that fixed i-frames in <see cref="MakeInvincible"/>.
    /// </summary>
    private void OnDisable()
    {
        if (holders.Count == 0) return;

        holders.Clear();
        WriteFlag(true);
    }

    /// <summary>
    /// One bool read per frame while a lock is held, and nothing at all when none is.
    /// See point 3 of the class summary: the third-party flag is the truth, this bookkeeping is not.
    /// </summary>
    private void Update()
    {
        if (holders.Count == 0 || controller == null) return;

        // Consistent: we hold locks and the flag is frozen. Nothing to adopt.
        if (!controller.GetInputAviable()) return;

        holders.Clear();
        if (!adoptedForeignUnlock)
        {
            adoptedForeignUnlock = true;
            Debug.LogWarning($"{name} was still holding movement locks when something outside this class freed " +
                "movement (the death animation's InputAfterDeath event fires straight at the StarterAssets " +
                "controller), so those locks were dropped instead of being kept as debt. If the player can now " +
                "move somewhere they should not, that system is using the wrong door.", this);
        }
    }

    /// <summary>
    /// Freezes the player's movement on behalf of <paramref name="holder"/>. Already holding a lock is a
    /// no-op, so a re-enterable ability (the LightAttack combo) can call this on every swing.
    /// </summary>
    public void AcquireInputLock(string holder)
    {
        if (string.IsNullOrEmpty(holder))
        {
            Debug.LogError($"{name} was asked to freeze movement for an empty holder name, which could never be " +
                "released. Pass the ability's AbilityIds name, or a LockOwner constant.", this);
            return;
        }

        if (holders.Add(holder)) WriteFlag(false);
    }

    /// <summary>
    /// Releases <paramref name="holder"/>'s lock. Movement returns only when the last holder lets go, so an
    /// interrupted ability can never hand back control another system is still entitled to. Releasing a lock
    /// this object does not hold is a no-op, because a clip whose restoring event fired twice must not warn.
    /// </summary>
    public void ReleaseInputLock(string holder)
    {
        if (string.IsNullOrEmpty(holder))
        {
            Debug.LogError($"{name} was asked to release the movement lock of an empty holder name.", this);
            return;
        }

        if (holders.Remove(holder)) WriteFlag(holders.Count == 0);
    }

    /// <summary>Drops every lock at once and frees movement - what the legacy EnableInput has always done.</summary>
    public void ReleaseAllInputLocks()
    {
        holders.Clear();
        WriteFlag(true);
    }

    /// <summary>
    /// Enables input for the player controller.
    /// This allows the player to receive input from the game's input system.
    ///
    /// ANIMATION EVENT TARGET (HitShield.anim): the name is data, do not rename it. It drops every lock on
    /// purpose, exactly as it always did - a guard hit restoring movement mid-ability is shipped behaviour,
    /// not something T-001 set out to change.
    /// </summary>
    public void EnableInput()
    {
        ReleaseAllInputLocks();

        if (playerInput != null) playerInput.ActivateInput();
        else ReportMissingPlayerInput("ActivateInput");
    }

    /// <summary>
    /// Disables input for the player controller.
    /// This prevents the player from receiving input, useful for:
    /// - Menu states
    /// - Sleep states
    /// - Death states
    ///
    /// ANIMATION EVENT TARGET (HitShield.anim): the name is data, do not rename it. It locks under
    /// <see cref="LockOwner.UiOrCutscene"/>, so an ability that happens to end during a cutscene no longer
    /// hands movement back to the player on its way out (it used to, through the shared bool).
    /// </summary>
    public void DisableInput()
    {
        AcquireInputLock(LockOwner.UiOrCutscene);

        if (playerInput != null) playerInput.DeactivateInput();
        else ReportMissingPlayerInput("DeactivateInput");
    }

    // ------------------------------------------------------------------
    //  Facade for callers that only have the character GameObject: the ability specs (plain classes, not
    //  components) and the health response. One GetComponent per ability activation, and a loud error
    //  instead of a NullReferenceException when a character is wired without an InputController.
    // ------------------------------------------------------------------

    /// <summary>Freezes the movement of <paramref name="character"/> on behalf of <paramref name="holder"/>.</summary>
    public static void AcquireInputLock(GameObject character, string holder)
    {
        InputController input = Resolve(character, holder);
        if (input != null) input.AcquireInputLock(holder);
    }

    /// <summary>Releases <paramref name="holder"/>'s lock on <paramref name="character"/>.</summary>
    public static void ReleaseInputLock(GameObject character, string holder)
    {
        InputController input = Resolve(character, holder);
        if (input != null) input.ReleaseInputLock(holder);
    }

    static InputController Resolve(GameObject character, string holder)
    {
        if (character == null)
        {
            // The character is gone: nothing left to lock or unlock, and the object that held the lock is
            // being torn down with it. An explained bail, not a swallowed bug.
            return null;
        }

        InputController input = character.GetComponent<InputController>();
        if (input == null)
        {
            Debug.LogError($"{character.name} is asked to change the movement lock of '{holder}' but has no " +
                "InputController component. Add one to the character root: without it this caller freezes and " +
                "unfreezes nothing, and the ability that owns it runs with no movement lock at all.", character);
        }
        return input;
    }

    /// <summary>
    /// The only first-party line of code allowed to touch the third-party movement flag.
    /// </summary>
    private void WriteFlag(bool available)
    {
        // Awake() owns the normal lookup. Retry once here so a component built outside the normal
        // lifecycle (edit-mode tooling, an ability run against an object that was inactive when it got
        // its components) can never make a lock quietly do nothing.
        if (controller == null) controller = GetComponent<ThirdPersonController>();

        if (controller == null)
        {
            if (!missingControllerReported)
            {
                missingControllerReported = true;
                Debug.LogError($"{name} has an InputController but no ThirdPersonController, so no movement lock " +
                    "can be applied: the character keeps walking through dashes, throws, parries and death " +
                    "animations. Both components belong on the character root.", this);
            }
            return;
        }

        // Intentional and contained: the misspelled setter of the read-only StarterAssets controller
        // (Assets/DownloadPackages/StarterAssets/ThirdPersonController/Scripts/ThirdPersonController.cs).
        controller.SetInputAviable(available);
    }

    private void ReportMissingPlayerInput(string action)
    {
        Debug.LogError($"{name} has an InputController with no PlayerInput assigned, so {action}() could not run: " +
            "the movement lock changed but the rest of the input system did not follow it. Assign the field.", this);
    }
}
