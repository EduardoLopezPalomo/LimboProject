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
