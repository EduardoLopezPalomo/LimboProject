using System;
using UnityEngine;

/// <summary>
/// PlayerWeaponController extends WeaponController with player-specific logic.
/// Wires the weapon's damage component and drives its collision window.
/// </summary>
public class PlayerWeaponController : WeaponController
{
    /// <summary>
    /// Reference to the player's weapon damage component.
    /// Used for damage calculations and hit detection.
    /// </summary>
    [HideInInspector] public PlayerWeaponDamage WeaponDamage;

    // T-042: this controller used to own `counterAttackChance` and push it onto the weapon at Awake, which
    // meant every enemy on screen shared one flat coin flip that never changed. The chance is enemy state
    // now: CounterAttackProfile holds the tunables, EnemyCounterAttackState holds the live value per
    // instance. There is deliberately nothing to configure here any more.

    protected override void Awake()
    {
        base.Awake();
        
        // Get the player's weapon damage component
        WeaponDamage = weapon.GetComponent<PlayerWeaponDamage>();
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
