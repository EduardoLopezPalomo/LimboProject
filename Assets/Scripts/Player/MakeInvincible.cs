using UnityEngine;

public class MakeInvincible : MonoBehaviour
{
    bool isInvincible = false;

    public void SetInvincible(bool value)
    {
        isInvincible = value;
    }

    public bool IsInvincible()
    {
        return isInvincible;
    }
}
