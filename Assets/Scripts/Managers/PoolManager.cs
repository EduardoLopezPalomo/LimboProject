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

    void Start()
    {
        if (DayNight.Instance != null)
        {
            WakeUp(DayNight.Instance.ActualPhase);
        }
    }

    void OnEnable()
    {
        DayNight.OnDayPhaseChanged += WakeUp;
    }

    void OnDisable()
    {
        DayNight.OnDayPhaseChanged -= WakeUp;
    }

    public void Sleep(NPCPool npc)
    {
        if (npc == null) return;

        npc.GoToSleep();
    }

    private void WakeUp(DayPhase newPhase)
    {
        if (newPhase == DayPhase.FirstLight)
        {
            for (int i = 0; i < Npcs.Count; i++)
            {
                if (Npcs[i] != null)
                    Npcs[i].WakeUp();
            }
        }
        else if (newPhase == DayPhase.Night)
        {
            for (int i = 0; i < Npcs.Count; i++)
            {
                if (Npcs[i] != null)
                    Npcs[i].GoToSleep();
            }
        }
    }
}
