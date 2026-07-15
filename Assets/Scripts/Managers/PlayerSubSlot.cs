using UnityEngine;

public class PlayerSubSlot : MonoBehaviour
{
    private void Awake()
    {
        if (SlotsManager.Instance != null)
        {
            SlotsManager.Instance.SetPlayer(transform);
        }
    }
}
