using UnityEngine;
using Unity.Behavior;
using UnityEngine.AI;

public class NPCGraphBridge : MonoBehaviour
{

    private BehaviorGraphAgent agent;
    private DialogueNPC dialogueNPC;
    private bool HasReachedHome = false;

    private NPCState stateBeforeDialogue = NPCState.Default;
    private bool inDialogue = false;
    private NavMeshAgent agentComponent;
    private Animator animator;

    void Awake()
    {
        agent = GetComponent<BehaviorGraphAgent>();
        dialogueNPC = GetComponentInParent<DialogueNPC>();
        animator = GetComponent<Animator>();
        agentComponent = GetComponent<NavMeshAgent>();
    }
    void Start()
    {
        if (DayNight.Instance != null)
        {
            HandleDayPhaseChanged(DayNight.Instance.CurrentPhase);
        }
        if (agentComponent != null && animator != null)
        {
            agentComponent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            animator.cullingMode = AnimatorCullingMode.CullCompletely;
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

        NPCState phaseState = NPCState.Default;
        bool knowsThisPhase = true;
        switch (newPhase)
        {
            case DayPhase.Dawn:
                phaseState = NPCState.GoingToWork;
                break;
            case DayPhase.Day:
                phaseState = NPCState.Working;
                break;
            case DayPhase.Sunset:
                phaseState = NPCState.StartingToRest;
                break;
            case DayPhase.Night:
                phaseState = NPCState.Resting;
                break;
            default:
                // FirstLight and Null have no state of their own: NPCPool switches the NPC on at
                // FirstLight and Dawn follows 0.3 s later, so leaving the previous state is what the
                // graphs were already getting. Recorded in T-057 candidate 8 as a design gap, not a crash.
                knowsThisPhase = false;
                break;
        }

        if (!knowsThisPhase)
        {
            return;
        }

        agent.BlackboardReference.SetVariableValue("NPCState", phaseState);

        // T-057 E2: while a conversation is open the graph is parked in Talking and the state it came
        // from is held in stateBeforeDialogue. A phase change is the truth for the new phase, so the
        // parked value has to follow it - otherwise closing the conversation rolled the NPC back to the
        // previous phase's state and left it there until the next change. Measured before the fix:
        // saved Working, phase forced to Night mid-conversation, restore put the NPC back to Working.
        if (inDialogue)
        {
            stateBeforeDialogue = phaseState;
        }
    }

    public void SetTalking(DialogueInitData data)
    {
        if (agent == null || dialogueNPC == null) return;

        // T-057 E1: dialogue is addressed by npcId, and an empty id matched every other empty id.
        // InternalDialogueTrigger raises "" for the protagonist's monologues, and D_Gandalf - the road
        // guard, and the project's only GuardBlockerNPC - shipped with NpcId "" too, so walking into a
        // monologue trigger hijacked that guard although nobody had spoken to it. An unaddressed event
        // must not move any NPC's state.
        if (string.IsNullOrEmpty(data.npcId) || data.npcId != dialogueNPC.NpcId) return;

        // T-057 E1: the save/restore is not re-entrant. A second hit while already talking captured
        // NPCState.Talking into stateBeforeDialogue and destroyed the real value, so the NPC came back
        // from the conversation *into* Talking with nothing able to free it but the next phase change
        // (measured: Default -> Working -> Talking -> Talking -> Talking).
        if (inDialogue) return;

        agent.BlackboardReference.GetVariableValue<NPCState>("NPCState", out stateBeforeDialogue);
        inDialogue = true;
        agent.BlackboardReference.SetVariableValue("NPCState", NPCState.Talking);
    }

    public void HandleEndDialogue(string npcId)
    {
        if (agent == null || dialogueNPC == null) return;
        if (!inDialogue) return;
        if (string.IsNullOrEmpty(npcId) || npcId != dialogueNPC.NpcId) return;

        inDialogue = false;
        agent.BlackboardReference.SetVariableValue("NPCState", stateBeforeDialogue);
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
