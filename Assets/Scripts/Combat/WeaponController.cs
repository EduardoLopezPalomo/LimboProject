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
        if (weapon == null)
        {
            Debug.LogError($"[WeaponController:{name}] 'weapon' is not assigned. Equipping and " +
                "collision toggles will be refused until it is set on the prefab.");
            return;
        }

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

        if (weapon == null)
        {
            Debug.LogError($"[WeaponController:{name}] EquipWeapon was called with no weapon assigned.");
            return;
        }

        if (RightHandSlot == null)
        {
            Debug.LogError($"[WeaponController:{name}] 'RightHandSlot' is not assigned, so " +
                $"'{weapon.name}' cannot be equipped.");
            return;
        }

        equipped = true;
        weapon.transform.SetParent(RightHandSlot.transform);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }

    public virtual void UnequipWeapon()
    {
        if (!equipped) return;

        if (weapon == null)
        {
            Debug.LogError($"[WeaponController:{name}] UnequipWeapon was called with no weapon assigned.");
            return;
        }

        if (HipSlot == null)
        {
            Debug.LogError($"[WeaponController:{name}] 'HipSlot' is not assigned, so " +
                $"'{weapon.name}' cannot be holstered.");
            return;
        }

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
