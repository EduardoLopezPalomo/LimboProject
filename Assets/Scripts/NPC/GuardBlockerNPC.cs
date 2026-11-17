using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Serialization;

public class GuardBlockerNPC : MonoBehaviour, IConversation
{
    [Header("Colliders")]
    [FormerlySerializedAs("colliderBloqueo")]
    [SerializeField] private Collider blockCollider;    // sólido (Is Trigger = false), bloquea el paso físicamente
    [FormerlySerializedAs("colliderDialogoAuto")]
    [SerializeField] private Collider autoDialogueTrigger; // trigger, dispara el diálogo al acercarte

    [Header("Diálogo mientras bloquea (reutiliza el NPC normal)")]
    [SerializeField] private DialogueNPC dialogueNPC;

    [Header("Condición para dejar de bloquear")]
    [SerializeField] private Quest quest;

    [Header("Qué hacer al liberar el paso")]
    [FormerlySerializedAs("desaparecerAlLiberar")]
    [SerializeField] private bool vanishOnRelease = true; // true = más simple, se esfuma del todo
    [FormerlySerializedAs("lineasDespedida")]
    [SerializeField] private List<string> farewellLines;     // solo se usa si vanishOnRelease = false
    [FormerlySerializedAs("nombreDespedida")]
    [SerializeField] private string farewellName;
    [FormerlySerializedAs("retratoDespedida")]
    [SerializeField] private NpcPortrait farewellPortrait;
    [FormerlySerializedAs("vozDespedida")]
    [SerializeField] private VoiceType farewellVoice;

    private bool hasBeenReleased = false;
    private bool dialogueAlreadyStarted = false;

    private void OnEnable()
    {
        Quest.OnQuestComplete += CheckQuest;
    }

    private void OnDisable()
    {
        Quest.OnQuestComplete -= CheckQuest;
    }

    private void Start()
    {
        CheckState(); 
    }

    private void CheckState()
    {
        if (quest == null)
        {
            Debug.LogError("GuardBlockerNPC on '" + name + "' has no quest assigned, so it can never let the player through.", this);
            return;
        }

        QuestManager manager = QuestManager.instance;
        if (manager == null)
        {
            Debug.LogWarning("No QuestManager in the scene, so GuardBlockerNPC on '" + name + "' cannot decide whether to unblock; staying blocked.", this);
            return;
        }

        // T-057 candidate 3: this used to test "is FollowTheMusic the CURRENT quest", so the release only
        // held while that quest was still active. The shipped guard runs vanishOnRelease = false, meaning
        // it stays in the world with its collider merely disabled - and on re-entering Village after the
        // story moved on, Start() no longer matched, the collider came back enabled and the guard blocked
        // the road for good. Release on "this quest has been finished".
        if (!manager.IsMainQuestCompleted(quest.questId))
        {
            return;
        }

        hasBeenReleased = true;
        if (blockCollider != null)
        {
            blockCollider.enabled = false;
        }

        if (vanishOnRelease)
        {
            Destroy(gameObject);
        }
    }

    private void CheckQuest(Quest completedQuest)
    {
        StartCoroutine(nextFrame());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || dialogueAlreadyStarted) return;

        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput == null) return;

        DialogueInitData data = new DialogueInitData { input = playerInput };

        if (!hasBeenReleased)
        {
            if (dialogueNPC == null)
            {
                Debug.LogError("GuardBlockerNPC on '" + name + "' has no dialogueNPC, so it has nothing to say while blocking.", this);
                return;
            }

            data.lines = dialogueNPC.GetDialogueLines();
            data.npcId = dialogueNPC.NpcId;
            data.characterName = dialogueNPC.NpcName;
            data.voice = dialogueNPC.NpcVoice;
            data.portrait = dialogueNPC.Portrait;
        }
        else // solo llega aquí si vanishOnRelease = false
        {
            data.lines = farewellLines != null ? new List<string>(farewellLines) : new List<string>();
            if (data.lines.Count == 0)
            {
                // Nothing to say, so nothing to open: raising an empty dialogue would switch the player
                // into the UI map and close it again on the same frame.
                Debug.LogWarning("GuardBlockerNPC on '" + name + "' was released but has no farewellLines, so the farewell is skipped.", this);
                return;
            }

            data.npcId = "guard_despedida";
            data.characterName = farewellName;
            data.voice = farewellVoice;
            data.portrait = farewellPortrait;
        }


        dialogueAlreadyStarted = true;
        IConversation.RaiseDialogueInit(data);
        playerInput.SwitchCurrentActionMap("UI");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            dialogueAlreadyStarted = false; // permite que vuelva a dispararse si se aleja y vuelve a acercarse
    }

    IEnumerator nextFrame()
    {
        yield return null;
        CheckState();
    }
}