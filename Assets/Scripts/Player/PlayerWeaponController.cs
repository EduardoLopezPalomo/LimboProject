using System;
using UnityEngine;

public class PlayerWeaponController : WeaponController
{
    [HideInInspector] public PlayerWeaponDamage WeaponDamage;
    
    [Header("Counter-Attack Chance [0,1]")]
    [SerializeField] public float counterAttackChance = 0.5f;


    protected override void Awake()
    {
        base.Awake();
        WeaponDamage = weapon.GetComponent<PlayerWeaponDamage>();
        counterAttackChance = Mathf.Clamp(counterAttackChance, 0f, 1f);
        WeaponDamage.counterAttackChance = counterAttackChance;
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
    
    public void ResetTransform()
    {
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }
}
