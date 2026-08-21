
using UnityEngine;

public class EnemyShieldController :  WeaponController
{
    public override void UnequipWeapon()
    {
        if (!equipped) return;

        equipped = false;
        weapon.transform.SetParent(HipSlot.transform);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }
}
