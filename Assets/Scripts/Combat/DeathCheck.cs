using UnityEngine;

public class DeathCheck : MonoBehaviour
{
    [SerializeField]private EnemiesGroupContoller controller;
    private bool IsDeath = false;

    public void SetIsDeath(bool value)
    {
        IsDeath = value;
        if (IsDeath && controller != null)
        {
            controller.ReturnToken();
        }
    }

    public bool GetIsDeath()
    {
        return IsDeath;
    }
}
