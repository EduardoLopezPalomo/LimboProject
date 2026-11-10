using UnityEngine;

/// <summary>
/// MakeInvincible provides a simple way to enable/disable invincibility for the player.
/// This component allows other scripts to set and query the invincible state.
/// Common use cases:
/// - During healing animations
/// - After taking damage
/// - In menu states
/// </summary>
public class MakeInvincible : MonoBehaviour
{
    /// <summary>
    /// Current invincibility state.
    /// </summary>
    bool isInvincible = false;

    /// <summary>
    /// Safety net for the manual SetInvincible(true) / SetInvincible(false) pairing.
    ///
    /// The only writer today is <see cref="KnockBack"/>, which raises the flag and clears it at the
    /// end of a KnockBackOffsetTime-long coroutine. If the character is disabled while that coroutine
    /// is still running (death, respawn, scene or god-space transition) Unity kills the coroutine, the
    /// clearing line never runs, and the flag survives into the next life - a respawned player that
    /// cannot be hit. Clearing on both halves of the enable cycle means the flag can never outlive the
    /// state change that set it, and changes nothing for callers that pair correctly.
    /// </summary>
    private void OnEnable()
    {
        isInvincible = false;
    }

    private void OnDisable()
    {
        isInvincible = false;
    }

    /// <summary>
    /// Sets the invincibility state.
    /// Called by other systems (combat, healing, state machines).
    /// </summary>
    /// <param name="value">true to become invincible, false to lose invincibility</param>
    public void SetInvincible(bool value)
    {
        isInvincible = value;
    }

    /// <summary>
    /// Returns whether the player is currently invincible.
    /// </summary>
    /// <returns>True if invincible</returns>
    public bool IsInvincible()
    {
        return isInvincible;
    }
}
