using UnityEngine;
using UnityEngine.Animations;

public class BowController : MonoBehaviour
{
    public GameObject HipSlot;
    public GameObject RightHandSlot;
    public GameObject weapon;
    public ParentConstraint pc;
    
    private Animator animator;

    private void Awake()
    {
        // T-046 B9: GetComponent<Animator>() used to run inside Update, i.e. once per frame for every
        // archer on screen. The Animator never changes on a live instance, so resolve it once and
        // fail loudly if the enemy prefab is missing one instead of throwing every frame.
        animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogError($"{name} has a BowController but no Animator, so InCombo cannot be read and the bow is never equipped.", this);
    }

    void Update()
    {
        if (animator == null) return;

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
        AttachWeaponTo(RightHandSlot);
    }

    public void UnequipWeapon()
    {
        AttachWeaponTo(HipSlot);
    }

    /// <summary>
    /// Same end state as before - the weapon under that slot with zeroed local transform - but it no
    /// longer calls SetParent when the weapon is already in that slot. Update drove this every frame,
    /// and re-parenting to the parent a transform already has still rebuilds its world matrix and
    /// dirties the hierarchy. Passing worldPositionStays and then overwriting the locals produces the
    /// identical result either way, so skipping the redundant call changes nothing observable.
    /// </summary>
    private void AttachWeaponTo(GameObject slot)
    {
        Transform parent = slot.transform;
        if (weapon.transform.parent != parent)
            weapon.transform.SetParent(parent);

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
