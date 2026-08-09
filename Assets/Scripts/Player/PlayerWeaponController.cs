using System;
using UnityEngine;

public class PlayerWeaponController : WeaponController
{
    [HideInInspector] public PlayerWeaponDamage WeaponDamage;

    protected override void Awake()
    {
        base.Awake();
        WeaponDamage = weapon.GetComponent<PlayerWeaponDamage>();
    }

    public override void SetComboNumber(int number)
    {
        WeaponDamage.SetComboNumber(number);
    }

    public override void EnableCollision()
    {
        WeaponDamage.ActivateHitbox();
    }
    
    public override void DisableCollision()
    {
        WeaponDamage.DeactivateHitbox();
    }
}
