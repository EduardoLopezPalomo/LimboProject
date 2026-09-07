using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class GuardBlockerNPC : MonoBehaviour, IConversation
{
    [Header("Colliders")]
    [SerializeField] private Collider colliderBloqueo;    // sólido (Is Trigger = false), bloquea el paso físicamente
    [SerializeField] private Collider colliderDialogoAuto; // trigger, dispara el diálogo al acercarte

    [Header("Diálogo mientras bloquea (reutiliza el NPC normal)")]
    [SerializeField] private DialogueNPC dialogueNPC;

    [Header("Condición para dejar de bloquear")]
    [SerializeField] private Quest quest;

    [Header("Qué hacer al liberar el paso")]
    [SerializeField] private bool desaparecerAlLiberar = true; // true = más simple, se esfuma del todo
    [SerializeField] private List<string> lineasDespedida;     // solo se usa si desaparecerAlLiberar = false
    [SerializeField] private string nombreDespedida;
    [SerializeField] private Sprite retratoDespedida;
    [SerializeField] private TipoVoz vozDespedida;

    private bool yaLiberado = false;
    private bool dialogoYaDisparado = false;

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
        RevisarEstado(QuestManager.instance.GetActualQuest()); 
    }

    private void RevisarEstado(string id)
    {
        if(quest.questId == id)
        {
            yaLiberado = true;
            colliderBloqueo.enabled = false;
            if (desaparecerAlLiberar)
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
        if (!other.CompareTag("Player") || dialogoYaDisparado) return;

        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput == null) return;

        DialogueInitData data = new DialogueInitData { input = playerInput };

        if (!yaLiberado)
        {
            data.lineas = dialogueNPC.ObtenerLineasDialogo();
            data.npcId = dialogueNPC.NpcId;
            data.nombre = dialogueNPC.NpcName;
            data.voz = dialogueNPC.VozNPC;
            data.retrato = dialogueNPC.RetratoNPC;
        }
        else // solo llega aquí si desaparecerAlLiberar = false
        {
            data.lineas = new List<string>(lineasDespedida);
            data.npcId = "guard_despedida";
            data.nombre = nombreDespedida;
            data.voz = vozDespedida;
            data.retrato = retratoDespedida;
        }


        dialogoYaDisparado = true;
        IConversation.RaiseDialogueInit(data);
        playerInput.SwitchCurrentActionMap("UI");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            dialogoYaDisparado = false; // permite que vuelva a dispararse si se aleja y vuelve a acercarse
    }

    IEnumerator nextFrame()
    {
        yield return null;
        RevisarEstado(QuestManager.instance.GetActualQuest());
    }
}