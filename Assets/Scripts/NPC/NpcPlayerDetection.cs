using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class NpcPlayerDetection : MonoBehaviour, IConversation
{
    public static bool PossibleInteraction = false;

    [SerializeField] private GameObject interactUI;
    [SerializeField] private GameObject npc;
    [SerializeField] private DialogueNPC dialogueNPC;

    private GameObject player;
    private PlayerInput playerInput;
    private bool playerDetected = false;

    void Update()
    {
        if (playerDetected)
        {
            interactUI.SetActive(true);
            PossibleInteraction = true;

            if (playerInput != null && playerInput.currentActionMap.name != "UI")
            {
                InputAction interactAction = playerInput.currentActionMap.FindAction("Interact");

                if (interactAction != null && interactAction.WasPressedThisFrame())
                {
                    IniciarDialogo();
                }
            }
        }
    }

    private void IniciarDialogo()
    {
        DialogueInitData data = new DialogueInitData();
        data.lineas = dialogueNPC.ObtenerLineasDialogo();
        data.npcId = dialogueNPC.NpcId;
        data.nombre = dialogueNPC.NpcName;
        data.voz = dialogueNPC.VozNPC;
        data.retrato = dialogueNPC.RetratoNPC;
        data.input = playerInput;

        IConversation.RaiseDialogueInit(data);

        playerInput.SwitchCurrentActionMap("UI");
        interactUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerDetected = true;
            player = other.gameObject;
            playerInput = other.GetComponent<PlayerInput>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerDetected = false;
            interactUI.SetActive(false);
            PossibleInteraction = false;
        }
    }
}