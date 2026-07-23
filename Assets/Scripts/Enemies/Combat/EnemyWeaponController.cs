using System;
using UnityEngine;

public class EnemyWeaponController :  WeaponController
{
    [HideInInspector] public EnemyWeaponDamage WeaponDamage;

    protected override void Awake()
    {
        base.Awake();
        WeaponDamage = weapon.GetComponent<EnemyWeaponDamage>();
    }

    public override void SetComboNumber(int number)
    {
        WeaponDamage.SetComboNumber(number);
    }

    public void CounterAttack(bool value)
    {
        WeaponDamage.IsCounterAttack(value);
    }
}
