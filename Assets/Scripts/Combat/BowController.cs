using UnityEngine;
using UnityEngine.Animations;

public class BowController : MonoBehaviour
{
    public GameObject HipSlot;
    public GameObject RightHandSlot;
    public GameObject weapon;
    public ParentConstraint pc;
    
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

    public void EnableConstaint()
    {
        pc.enabled = true;
    }
    public void DisableConstaint()
    {
        pc.enabled = false;
        pc.transform.localPosition = new Vector3(-0.0014f, -0.0087f, -0.0004f);
        pc.transform.localRotation = Quaternion.identity;
    }
}
