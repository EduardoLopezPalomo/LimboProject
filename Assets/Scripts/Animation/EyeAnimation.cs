using System;
using UnityEngine;

public class EyeAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [SerializeField, Tooltip("Keep the eyelid playing while Time.timeScale is 0. MapToggle pauses the game as it opens the map, and an Animator does not advance at timeScale 0: without this the close never finishes, its animation event never fires, and the map wedges halfway open. Ignored while the game is running normally, so nothing else changes.")]
    private bool playWhilePaused = true;

    private Action onDone;

    // T-058 M2: the open/close chain that MapToggle runs ends at an Animation Event, and until now the
    // only thing that could notice one that never arrived was a player staring at a paused world. These
    // members make "a completion is armed, for which direction, for how long, and can it still happen"
    // readable from the outside, which is what lets MapToggle's watchdog name the stalled step instead
    // of guessing, and decide that an event with no Animator behind it is worth no patience at all.
    private string requestName;
    private float requestedUnscaledTime;

    /// <summary>True between a PlayOpen/PlayClose and the animation event that completes it.</summary>
    public bool HasPendingCallback => onDone != null;

    /// <summary>"Open" or "Close" while a completion is armed, null otherwise, so a caller can say
    /// which step of the chain stalled rather than just which component.</summary>
    public string PendingRequest => requestName;

    /// <summary>How long this request has been waiting, in UNSCALED seconds - the map pauses the game
    /// while its chain runs, so a scaled clock would sit at zero and never time out.</summary>
    public float PendingSeconds => onDone == null ? 0f : Time.unscaledTime - requestedUnscaledTime;

    /// <summary>False once no event can arrive any more: no Animator, or this GameObject switched off
    /// (the eye lives under the HUD, so a menu, a dialogue or a HUD fade takes it) or the component
    /// disabled. MapToggle reads this to stop waiting rather than to wait harder.</summary>
    public bool CanAnimate => animator != null && animator.isActiveAndEnabled && gameObject.activeInHierarchy;

    private void Update()
    {
        if (!playWhilePaused || animator == null) return;
        if (Time.timeScale >= 0.0001f) return;
        if (animator.isActiveAndEnabled == false || animator.gameObject.activeInHierarchy == false) return;

        animator.Update(Time.unscaledDeltaTime * animator.speed);
    }

    public void PlayClose(Action callback = null)
    {
        if (!BeginRequest("Close", callback)) return;

        animator.ResetTrigger("Open");
        animator.SetTrigger("Close");
    }

    public void PlayOpen(Action callback = null)
    {
        if (!BeginRequest("Open", callback)) return;

        animator.ResetTrigger("Close");
        animator.SetTrigger("Open");
    }

    /// <summary>
    /// Arms one direction's completion. Returns false - and has already run the callback, so the caller's
    /// chain COMPLETES instead of hanging - when this eye has no Animator to ask. That is T-058 M2's rule:
    /// a transition that cannot animate must still finish, because the alternative is a session paused at
    /// timeScale 0 with the camera handed to nobody, which is what an unassigned Animator used to buy.
    /// A second request while one is still pending is also reported here: onDone is a single slot, so the
    /// first caller's completion is what that silently drops (EyeTester and MapToggle already share eyes).
    /// </summary>
    private bool BeginRequest(string request, Action callback)
    {
        if (animator == null)
        {
            Debug.LogError($"{name} was asked to play the map eye's {request} but has no Animator assigned, so its animation event can never arrive. Running the completion now instead of wedging the caller (T-058 M2).", this);
            callback?.Invoke();
            return false;
        }

        if (onDone != null)
            Debug.LogWarning($"{name} was asked to {request} while still waiting for its {requestName} event, so that earlier request's completion has just been dropped. Two systems driving one eye (T-058 M2 case 2).", this);

        onDone = callback;
        requestName = request;
        requestedUnscaledTime = Time.unscaledTime;
        return true;
    }

    /// <summary>Drops the armed completion WITHOUT running it, for a caller that has decided to finish the
    /// transition itself (MapToggle's watchdog). The real event may still turn up late; with nothing armed
    /// it lands on nothing instead of running a chain that has already been completed.</summary>
    public void CancelPendingCallback()
    {
        onDone = null;
        requestName = null;
    }

    // Salta directamente al último fotograma de cada animación
    public void SetClosedInstant() => PlayInstant("Close");
    public void SetOpenInstant() => PlayInstant("Open");

    /// <summary>Jumps a direction's state to its last frame. Guarded for the same reason as BeginRequest:
    /// MapToggle calls this on the eye it is about to animate, and an unassigned Animator used to throw
    /// halfway through the open chain - after the pause and the camera lock, before the state change.</summary>
    private void PlayInstant(string stateName)
    {
        if (animator == null)
        {
            Debug.LogError($"{name} was told to jump straight to its {stateName} pose but has no Animator assigned, so the eyelid keeps whatever pose it has. Assign one (T-058 M2).", this);
            return;
        }

        animator.Play(stateName, 0, 1f);
    }

    // Los llaman los Animation Events (último fotograma de cada clip)
    public void OnCloseFinished() => Finish();
    public void OnOpenFinished() => Finish();

    private void Finish()
    {
        Action cb = onDone;
        onDone = null;
        requestName = null;
        cb?.Invoke();
    }
}