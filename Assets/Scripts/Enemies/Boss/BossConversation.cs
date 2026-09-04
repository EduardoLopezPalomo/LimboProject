using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.InputSystem;

public class BossConversation : MonoBehaviour, IConversation
{
    [SerializeField] private GameObject Boss;
    [SerializeField] private NPCDialogueData dataIntro;
    [SerializeField] private NPCDialogueData dataBeaten;
    [SerializeField] private DialogueNPC dialogueNPC;
    [SerializeField] private Quest actualQuest;
    [SerializeField] private Goals actualGoal;
    [SerializeField] private BoxCollider teleportCollider;

    private BoxCollider boxCollider;
    private PlayerInput playerInput;
    private BehaviorGraphAgent agent;
    private bool BattleStarted = false;
    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        agent = Boss.GetComponent<BehaviorGraphAgent>();
    }

    private void OnEnable()
    {
        DialogueUI.OnEndDialogue += EndDialogue;
        EnemyGraphBridge.OnDeath += BossDeath;
    }

    private void OnDisable()
    {
        DialogueUI.OnEndDialogue -= EndDialogue;
        EnemyGraphBridge.OnDeath -= BossDeath;
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("actualQuest: " + actualQuest.questId + " actualGoal: " + actualGoal.completed + " QuestManager.instance.GetActualQuest(): " + QuestManager.instance.GetActualQuest());
        if (actualQuest == null || QuestManager.instance.GetActualQuest() != actualQuest.questId || actualGoal.completed) return;

        if (other.CompareTag("Player"))
        {
            playerInput = other.GetComponent<PlayerInput>();
            DialogueInitData data = new DialogueInitData();
            data.lineas = dataIntro.dayGreeting;
            data.npcId = dialogueNPC.NpcId;
            data.nombre = dataIntro.NpcName;
            data.voz = dialogueNPC.VozNPC;
            data.retrato = dialogueNPC.RetratoNPC;
            data.input = playerInput;

            IConversation.RaiseDialogueInit(data);

            playerInput.SwitchCurrentActionMap("UI");
            boxCollider.enabled = false;
            teleportCollider.enabled = false;
        }
    }

    private void EndDialogue(string NpcId)
    {
        if (NpcId != dataIntro.NpcId) return;

        if (!BattleStarted)
        {
            agent.BlackboardReference.SetVariableValue("StartAttack", true);
            BattleStarted = true;
        }
        else
        {
            Destroy(Boss, 0.5f);
        }

    }

    private void BossDeath(GameObject deathEnemy)
    {
        if (deathEnemy == Boss && playerInput)
        {
            DialogueInitData data = new DialogueInitData();
            data.lineas = dataBeaten.dayGreeting;
            data.npcId = dialogueNPC.NpcId;
            data.nombre = dataBeaten.NpcName;
            data.voz = dialogueNPC.VozNPC;
            data.retrato = dialogueNPC.RetratoNPC;
            data.input = playerInput;

            IConversation.RaiseDialogueInit(data);

            playerInput.SwitchCurrentActionMap("UI");
            teleportCollider.enabled = true;
        }
    }
}
