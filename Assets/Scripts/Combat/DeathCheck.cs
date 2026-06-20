using UnityEngine;

public class DeathCheck : MonoBehaviour
{
    private bool IsDeath = false;

    public void SetIsDeath(bool value)
    {
        IsDeath = value;
    }

    public bool GetIsDeath()
    {
        return IsDeath;
    }
}
