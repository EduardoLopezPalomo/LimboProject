using UnityEngine;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{

    [SerializeField] private List<NPCPool> Npcs = new();
    public static PoolManager Instance { get; private set; }

    public Transform player;
    public float activeDistance = 20f;
    public float hysteresis = 5f;

    int currentIndex = 0;
    public int npcsCheckedPerFrame = 5;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Update()
    {
        int count = Mathf.Min(npcsCheckedPerFrame, Npcs.Count);
        for (int i = 0; i < count; i++)
        {
            currentIndex = (currentIndex + 1) % Npcs.Count;
            CheckNPC(Npcs[currentIndex]);
        }
    }

    void CheckNPC(NPCPool entry)
    {
        if (entry.Npc == null) return;

        float dist = Vector3.Distance(entry.Npc.transform.position, player.position);
        bool isActive = entry.IsPoolActive;

        if (isActive && dist > activeDistance + hysteresis)
        {
            entry.Deactivate();
        }
        else if (!isActive && dist < activeDistance)
        {
            entry.Activate();
        }
    }

    public void Sleep(NPCPool npc)
    {
        if (npc == null) return;

        npc.GoToSleep();
    }
}
