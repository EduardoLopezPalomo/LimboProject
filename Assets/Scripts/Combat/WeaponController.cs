using System;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public GameObject HipSlot;
    public GameObject RightHandSlot;
    public GameObject weapon;

    private WeaponDamage weaponDamage;
    private Animator animator;
    private bool equipped = false;

    private void Awake()
    {
        weaponDamage = weapon.GetComponent<WeaponDamage>();
        animator = GetComponent<Animator>();
    }
    public void EnableCollision(int comboNumber)
    {
        BoxCollider collider = weapon.GetComponent<BoxCollider>();
        collider.enabled = true;
        weaponDamage.SetComboNumber(comboNumber);
    }

    public void DisableCollision()
    {
        BoxCollider collider = weapon.GetComponent<BoxCollider>();
        collider.enabled = false;
    }

    public void EquipWeapon()
    {
        if (equipped) return;

        equipped = true;
        weapon.transform.SetParent(RightHandSlot.transform);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }

    public void UnequipWeapon()
    {
        if (!equipped) return;

        equipped = false;
        weapon.transform.SetParent(HipSlot.transform);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }
}
