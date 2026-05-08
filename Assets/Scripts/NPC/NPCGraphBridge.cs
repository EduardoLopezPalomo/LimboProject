using UnityEngine;
using Unity.Behavior;

public class NPCGraphBridge : MonoBehaviour
{

    private BehaviorGraphAgent agent;
    private bool HasReachedHome = false;

    void Awake()
    {
        agent = GetComponent<BehaviorGraphAgent>();
    }
    void Start()
    {
        if (DayNight.Instance != null)
        {
            HandleDayPhaseChanged(DayNight.Instance.ActualPhase);
        }
    }

    void OnEnable()
    {
        DayNight.OnDayPhaseChanged += HandleDayPhaseChanged;
    }

    void OnDisable()
    {
        DayNight.OnDayPhaseChanged -= HandleDayPhaseChanged;
    }

    private void HandleDayPhaseChanged(DayPhase newPhase)
    {
        if (agent == null) return;
        switch (newPhase)
        {
            case DayPhase.Dawn:
                agent.BlackboardReference.SetVariableValue("NPCState", NPCState.GoingToWork);
                break;
            case DayPhase.Day:
                agent.BlackboardReference.SetVariableValue("NPCState", NPCState.Working);
                break;
            case DayPhase.Sunset:
                agent.BlackboardReference.SetVariableValue("NPCState", NPCState.StartingToRest);
                break;
            case DayPhase.Night:
                agent.BlackboardReference.SetVariableValue("NPCState", NPCState.Resting);
                break;
        }
    }

    public void SetTalking()
    {
        if (agent != null)
        {
            agent.BlackboardReference.SetVariableValue("NPCState", NPCState.Talking);
        }
    }

    public void SetReachedHome(bool HasReached)
    {
        HasReachedHome = HasReached;
    }

    public bool GetReachedHome()
    {
        return HasReachedHome;
    }
}
