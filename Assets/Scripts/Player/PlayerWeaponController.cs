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
}
