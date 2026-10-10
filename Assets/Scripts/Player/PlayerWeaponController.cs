using System;
using UnityEngine;

/// <summary>
/// PlayerWeaponController extends WeaponController with player-specific logic.
/// Handles counter-attack mechanics and integration with PlayerWeaponDamage.
/// </summary>
public class PlayerWeaponController : WeaponController
{
    /// <summary>
    /// Reference to the player's weapon damage component.
    /// Used for damage calculations and hit detection.
    /// </summary>
    [HideInInspector] public PlayerWeaponDamage WeaponDamage;
    
    [Header("Counter-Attack Chance [0,1]")]
    /// <summary>
    /// Probability of landing a counter-attack when attacking from the back.
    /// Value is clamped between 0.0 and 1.0.
    /// Default: 0.5 (50% chance)
    /// </summary>
    [SerializeField] public float counterAttackChance = 0.5f;

    protected override void Awake()
    {
        base.Awake();
        
        // Get the player's weapon damage component
        WeaponDamage = weapon.GetComponent<PlayerWeaponDamage>();
        
        // Clamp counter-attack chance to valid range
        counterAttackChance = Mathf.Clamp(counterAttackChance, 0f, 1f);
        
        // Sync with the weapon damage component
        WeaponDamage.counterAttackChance = counterAttackChance;
    }

    /// <summary>
    /// Sets the current combo number.
    /// Used for combo damage calculations.
    /// </summary>
    /// <param name="number">Current combo count</param>
    public override void SetComboNumber(int number)
    {
        WeaponDamage.SetComboNumber(number);
    }

    /// <summary>
    /// Enables hitbox collision for the weapon.
    /// Activates the damage hitbox for collision detection.
    /// </summary>
    public override void EnableCollision()
    {
        WeaponDamage.ActivateHitbox();
    }
    
    /// <summary>
    /// Disables hitbox collision for the weapon.
    /// Deactivates the damage hitbox.
    /// </summary>
    public override void DisableCollision()
    {
        WeaponDamage.DeactivateHitbox();
    }
    
    /// <summary>
    /// Resets the weapon's local transform to zero.
    /// Used for animation resets or weapon returns.
    /// </summary>
    public void ResetTransform()
    {
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }
}
