using UnityEngine;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{

    [SerializeField] private List<NPCPool> Npcs = new();
    public static PoolManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Sleep(NPCPool npc)
    {
        if (npc == null) return;

        npc.GoToSleep();
    }
}
