using System;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public GameObject HipSlot;
    public GameObject RightHandSlot;
    public GameObject weapon;
    
    protected bool equipped = false;
    private BoxCollider weaponCollider;
    private SphereCollider sphereCollider;

    protected virtual void Awake()
    {
        weaponCollider = weapon.GetComponent<BoxCollider>();
        sphereCollider = weapon.GetComponent<SphereCollider>();
    }

    public virtual void EnableCollision()
    {
        if (weaponCollider)
        {
            weaponCollider.enabled = true;
        }

        if (sphereCollider)
        {
            sphereCollider.enabled = true;
        }
    }

    public virtual void DisableCollision()
    {
        if (weaponCollider)
        {
            weaponCollider.enabled = false;     
        }
        
        if (sphereCollider)
        {
            sphereCollider.enabled = false;
        }
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
