using System;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public GameObject HipSlot;
    public GameObject RightHandSlot;
    public GameObject weapon;
    
    private bool equipped = false;
    private BoxCollider weaponCollider;

    protected virtual void Awake()
    {
        weaponCollider = weapon.GetComponent<BoxCollider>();
    }

    public virtual void EnableCollision()
    {
        weaponCollider.enabled = true;
    }

    public virtual void DisableCollision()
    {
        weaponCollider.enabled = false;
    }

    public virtual void EquipWeapon()
    {
        if (equipped) return;

        equipped = true;
        weapon.transform.SetParent(RightHandSlot.transform);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }

    public virtual void UnequipWeapon()
    {
        if (!equipped) return;

        equipped = false;
        DisableCollision();
        weapon.transform.SetParent(HipSlot.transform);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }

    public virtual void SetComboNumber(int number)
    {
        
    }
}
