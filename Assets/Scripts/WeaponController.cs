using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public GameObject weapon;

    public void EnableCollision()
    {
        BoxCollider collider = weapon.GetComponent<BoxCollider>();
        collider.enabled = true;
    }

    public void DisableCollision()
    {
        BoxCollider collider = weapon.GetComponent<BoxCollider>();
        collider.enabled = false;
    }
}
