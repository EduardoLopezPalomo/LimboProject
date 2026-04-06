using System;
using UnityEngine;
using UnityEngine.UIElements;

public class GoingToPlayer : MonoBehaviour
{
    [SerializeField] private GameObject player;
    public float offsetPosiiton = 15f;
    public SlotType slotType;
    
    private Slot currentSlot;

    public bool IsPlayerInRange()
    {
        return Vector3.Distance(transform.position, player.transform.position) <= offsetPosiiton;
    }

    public Vector3 GetPlayerPosition()
    {
        Vector3 direction = (transform.position - player.transform.position).normalized;
        Vector3 Position = player.transform.position + direction * offsetPosiiton;


        return Position;
    }

    public Vector3 GetSlotPosition()
    {
        if (currentSlot != null)
        {
            return currentSlot.position;
        }
        
        currentSlot = SlotsManager.Instance.GetBestFreeSlot(gameObject, slotType);
        
        if (currentSlot != null)
        {
            return currentSlot.position;
        }
        Debug.LogWarning("No slot found");
        return GetPlayerPosition();
    }
    
    public void ReleaseSlot()
    {
        if (currentSlot != null)
        {
            SlotsManager.Instance.ReleaseSlot(currentSlot);
            currentSlot = null;
        }
    }
}
