using System.Collections.Generic;
using UnityEngine;

public class SlotsManager : MonoBehaviour
{
    public static SlotsManager Instance { get; private set; }
    
    [Header("Slots around player for enemies")]
    public Transform player;
    public float distanceToUpdate = 0.5f;
    [Header("Slots numbers")]
    public int closeSlots = 4;
    public int midSlots = 6;
    public int farSlots = 8;
    [Header("Slots radius")]
    public float closeRadius = 2f;
    public float midRadius = 4f;
    public float farRadius = 6f;
    [Header("Rotación de los anillos (carrusel)")]
    public float closeRingRotationSpeed = 5f; 
    public float midRingRotationSpeed = 7f;
    public float farRingRotationSpeed = 10f;

    private Vector3 LastPosition;
    
    private float closeRingAngle = 0f;
    private float midRingAngle = 0f;
    private float farRingAngle = 0f;
    
    private List<Slot> closeSlotList = new List<Slot>();
    private List<Slot> midSlotList = new List<Slot>();
    private List<Slot> farSlotList = new List<Slot>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            GenerateSlots();
            LastPosition = player.position;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
        LastPosition = player.position;
        
        UpdateRing(closeSlotList, closeRadius, closeRingAngle);
        UpdateRing(midSlotList, midRadius, midRingAngle);
        UpdateRing(farSlotList, farRadius, farRingAngle);
    }
    
    void Update()
    {
        if (player == null)
            return;
        
        closeRingAngle = WrapAngle(closeRingAngle + closeRingRotationSpeed * Time.deltaTime);
        midRingAngle = WrapAngle(midRingAngle + midRingRotationSpeed * Time.deltaTime);
        farRingAngle = WrapAngle(farRingAngle + farRingRotationSpeed * Time.deltaTime);
        
        UpdateRing(closeSlotList, closeRadius, closeRingAngle);
        UpdateRing(midSlotList, midRadius, midRingAngle);
        UpdateRing(farSlotList, farRadius, farRingAngle);
        
        if (Vector3.Distance(LastPosition, player.position) > distanceToUpdate)
        {
            LastPosition = player.position;
        }
    }
    
    private float WrapAngle(float angle)
    {
        if (angle > 360f) angle -= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }
 
    void UpdateRing(List<Slot> list, float radius, float ringAngle)
    {
        int count = list.Count;
        float ringAngleRad = ringAngle * Mathf.Deg2Rad;

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2 / count + ringAngleRad;
            Vector3 newPos = player.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            list[i].position = newPos;
        }
    }
    
    private void GenerateSlots()
    {
        closeSlotList.Clear();
        midSlotList.Clear();
        farSlotList.Clear();
        
        CreateRing(closeSlotList, closeSlots, closeRadius);
        CreateRing(midSlotList, midSlots, midRadius);
        CreateRing(farSlotList, farSlots, farRadius);
    }
    
    private void CreateRing(List<Slot> slots, int count, float radius)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2 / count;
            Vector3 position = player.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            slots.Add(new Slot(position));
        }
    }

    public Slot GetBestFreeSlot(GameObject requester, SlotType slotType )
    {
        Slot bestSlot = null;
        float bestDistance = float.MaxValue;
        List<Slot> slots = null;
        
        switch (slotType)
        {
            case SlotType.Close:
                slots = closeSlotList; 
                break;
            case SlotType.Mid:
                slots = midSlotList;
                break;
            case SlotType.Far:
                slots = farSlotList;
                break;
        }
        
        foreach (Slot slot in slots)
        {
            if (slot.IsAvailable())
            {
                float distance = Vector3.Distance(requester.transform.position, slot.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestSlot = slot;
                }
            }
        }
        
        if (bestSlot != null)
        {
            bestSlot.reservedBy = requester;
        }
        
        return bestSlot;
    }
    
    public void ReleaseSlot(Slot slot)
    {
        if (slot.reservedBy != null)
        {
            slot.reservedBy = null;    
        }
    }
    /*Function only for debug*/
    void OnDrawGizmos()
    {
        if (closeSlotList == null || midSlotList == null || farSlotList == null ) return;

        foreach (var slot in closeSlotList)
        {
            if (slot == null) continue;

            // Color según estado
            if (slot.reservedBy == null)
                Gizmos.color = Color.green; // libre
            else
                Gizmos.color = Color.red;   // ocupado

            Gizmos.DrawSphere(slot.position, 0.3f);
        }
        foreach (var slot in midSlotList)
        {
            if (slot == null) continue;

            // Color según estado
            if (slot.reservedBy == null)
                Gizmos.color = Color.green; // libre
            else
                Gizmos.color = Color.red;   // ocupado

            Gizmos.DrawSphere(slot.position, 0.3f);
        }
        foreach (var slot in farSlotList)
        {
            if (slot == null) continue;

            // Color según estado
            if (slot.reservedBy == null)
                Gizmos.color = Color.green; // libre
            else
                Gizmos.color = Color.red;   // ocupado

            Gizmos.DrawSphere(slot.position, 0.3f);
        }
    }

}
