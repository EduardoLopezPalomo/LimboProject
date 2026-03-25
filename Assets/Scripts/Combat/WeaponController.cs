using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public GameObject HipSlot;
    public GameObject RightHandSlot;
    public GameObject weapon;

    private WeaponDamage weaponDamage;

    private void Awake()
    {
        weaponDamage = weapon.GetComponent<WeaponDamage>();
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

    void Update()
    {
        Animator animator = GetComponent<Animator>();
        if (animator.GetBool("InCombo"))
        {
            EquipWeapon();
        }
        else
        {
            UnequipWeapon();
        }
    }
    public void EquipWeapon()
    {
        weapon.transform.SetParent(RightHandSlot.transform);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }

    public void UnequipWeapon()
    {
        weapon.transform.SetParent(HipSlot.transform);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }
}
