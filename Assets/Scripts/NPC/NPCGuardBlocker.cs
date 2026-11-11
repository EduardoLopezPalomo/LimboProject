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
        CheckState(QuestManager.instance.GetCurrentQuest()); 
    }

    private void CheckState(string id)
    {
        if(quest.questId == id)
        {
            hasBeenReleased = true;
            blockCollider.enabled = false;
            if (vanishOnRelease)
            {
                Destroy(gameObject);
            }
        }
    }

    private void CheckQuest(Quest quest)
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
            data.lines = dialogueNPC.GetDialogueLines();
            data.npcId = dialogueNPC.NpcId;
            data.characterName = dialogueNPC.NpcName;
            data.voice = dialogueNPC.NpcVoice;
            data.portrait = dialogueNPC.Portrait;
        }
        else // solo llega aquí si vanishOnRelease = false
        {
            data.lines = new List<string>(farewellLines);
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
        CheckState(QuestManager.instance.GetCurrentQuest());
    }
}