using UnityEngine;
using Unity.Behavior;
using UnityEngine.AI;

public class NPCGraphBridge : MonoBehaviour
{

    private BehaviorGraphAgent agent;
    private DialogueNPC dialogueNPC;
    private bool HasReachedHome = false;

    private NPCState stateBeforeDialogue = NPCState.Default;
    private NavMeshAgent agentComponent;

    void Awake()
    {
        agent = GetComponent<BehaviorGraphAgent>();
        dialogueNPC = GetComponentInParent<DialogueNPC>();
        agentComponent = GetComponent<NavMeshAgent>();
    }
    void Start()
    {
        if (DayNight.Instance != null)
        {
            HandleDayPhaseChanged(DayNight.Instance.ActualPhase);
        }
        if (agentComponent != null)
        {
            agentComponent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        }
    }

    void OnEnable()
    {
        DayNight.OnDayPhaseChanged += HandleDayPhaseChanged;
        IConversation.OnDialogueInit += SetTalking;
        DialogueUI.OnEndDialogue += HandleEndDialogue;
    }

    void OnDisable()
    {
        DayNight.OnDayPhaseChanged -= HandleDayPhaseChanged;
        IConversation.OnDialogueInit -= SetTalking;
        DialogueUI.OnEndDialogue -= HandleEndDialogue;
    }

    private void HandleDayPhaseChanged(DayPhase newPhase)
    {
        if (agent == null)
        {
            return;
        }
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

    public void SetTalking(DialogueInitData data)
    {
        if (agent != null && data.npcId == dialogueNPC.NpcId)
        {
            agent.BlackboardReference.GetVariableValue<NPCState>("NPCState", out stateBeforeDialogue);
            agent.BlackboardReference.SetVariableValue("NPCState", NPCState.Talking);
        }
    }

    public void HandleEndDialogue(string npcId)
    {
        if (agent != null && npcId == dialogueNPC.NpcId)
        {
            agent.BlackboardReference.SetVariableValue("NPCState", stateBeforeDialogue);
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
