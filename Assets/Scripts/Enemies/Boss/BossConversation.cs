using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Serialization;

public class BossConversation : MonoBehaviour, IConversation
{
    [SerializeField] private GameObject Boss;
    [SerializeField] private NPCDialogueData dataIntro;
    [SerializeField] private NPCDialogueData dataBeaten;
    [SerializeField] private DialogueNPC dialogueNPC;
    [FormerlySerializedAs("actualQuest")]
    [SerializeField] private Quest currentQuest;
    [FormerlySerializedAs("actualGoal")]
    [SerializeField] private Goals currentGoal;
    [SerializeField] private BoxCollider teleportCollider;

    [Header("Continuará")]
    [SerializeField] private GameObject continuaraUI; 
    [SerializeField] private float segundosAntesDeMenu = 3f;
    [SerializeField] private string creditsScene;

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
        if (currentQuest == null || QuestManager.instance.GetCurrentQuest() != currentQuest.questId || currentGoal.completed) return;

        if (other.CompareTag("Player"))
        {
            playerInput = other.GetComponent<PlayerInput>();
            DialogueInitData data = new DialogueInitData();
            data.lines = dataIntro.dayGreeting;
            data.npcId = dialogueNPC.NpcId;
            data.characterName = dataIntro.NpcName;
            data.voice = dialogueNPC.NpcVoice;
            data.portrait = dialogueNPC.Portrait;
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
            StartCoroutine(ShowContinueThenReturnToMenu());
        }

    }

    private void BossDeath(GameObject deathEnemy)
    {
        if (deathEnemy == Boss && playerInput)
        {
            DialogueInitData data = new DialogueInitData();
            data.lines = dataBeaten.dayGreeting;
            data.npcId = dialogueNPC.NpcId;
            data.characterName = dataBeaten.NpcName;
            data.voice = dialogueNPC.NpcVoice;
            data.portrait = dialogueNPC.Portrait;
            data.input = playerInput;

            IConversation.RaiseDialogueInit(data);

            playerInput.SwitchCurrentActionMap("UI");
            teleportCollider.enabled = true;
        }
    }

    private IEnumerator ShowContinueThenReturnToMenu()
    {
        yield return new WaitForSeconds(0.5f);

        if (continuaraUI != null)
            continuaraUI.SetActive(true);

        yield return new WaitForSeconds(segundosAntesDeMenu);

        LevelLoader.Instance.LoadLevelAsync(creditsScene);
    }
}
