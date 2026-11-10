using System;
using UnityEngine;

public class EnemyWeaponController :  WeaponController
{
    [HideInInspector] public EnemyWeaponDamage WeaponDamage;

    /// <summary>
    /// Set once Awake has reported that this controller has no damage component, so a weapon that
    /// legitimately has none (a shield) does not log again on every behaviour-graph call.
    /// </summary>
    private bool missingDamageReported = false;


    protected override void Awake()
    {
        base.Awake();

        if (weapon == null)
        {
            missingDamageReported = true;
            Debug.LogError($"[EnemyWeaponController:{name}] 'weapon' is not assigned, so this " +
                "controller has nothing to drive.");
            return;
        }

        WeaponDamage = weapon.GetComponent<EnemyWeaponDamage>();
        if (WeaponDamage == null)
        {
            missingDamageReported = true;
            Debug.LogWarning($"[EnemyWeaponController:{name}] '{weapon.name}' has no " +
                "EnemyWeaponDamage, so it cannot deal damage or be flagged as a counter attack.");
        }
    }

    public override void SetComboNumber(int number)
    {
        if (!HasDamage()) return;

        WeaponDamage.SetComboNumber(number);
    }

    public void CounterAttack(bool value)
    {
        if (!HasDamage()) return;

        WeaponDamage.IsCounterAttack(value);
    }

    /// <summary>
    /// Guards the two calls that used to throw a NullReferenceException the moment a behaviour graph
    /// touched a weapon without a damage component. The miss itself is reported once by Awake, so
    /// this stays quiet afterwards.
    /// </summary>
    private bool HasDamage()
    {
        if (WeaponDamage != null) return true;

        if (!missingDamageReported)
        {
            missingDamageReported = true;
            Debug.LogError($"[EnemyWeaponController:{name}] has no EnemyWeaponDamage; the call is " +
                "ignored.");
        }
        return false;
    }

}
