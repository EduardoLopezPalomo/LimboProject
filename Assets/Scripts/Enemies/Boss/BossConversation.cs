using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.InputSystem;

public class BossConversation : MonoBehaviour
{
    [SerializeField] private GameObject Boss;
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private NPCDialogueData dataIntro;
    [SerializeField] private NPCDialogueData dataBeaten;
    [SerializeField] private DialogueNPC dialogueNPC;
    
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
        if (other.CompareTag("Player"))
        {
            playerInput = other.GetComponent<PlayerInput>();
            dialogueUI.MostrarDialogo(
                dataIntro.dayGreeting,
                dataIntro.NpcName,
                dialogueNPC.VozNPC,
                dialogueNPC.RetratoNPC,
                playerInput);

            playerInput.SwitchCurrentActionMap("UI");
            boxCollider.enabled = false;
        }
    }

    private void EndDialogue()
    {
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
            dialogueUI.MostrarDialogo(
                dataBeaten.dayGreeting,
                dataBeaten.NpcName,
                dialogueNPC.VozNPC,
                dialogueNPC.RetratoNPC,
                playerInput);

            playerInput.SwitchCurrentActionMap("UI");
        }
    }
}
