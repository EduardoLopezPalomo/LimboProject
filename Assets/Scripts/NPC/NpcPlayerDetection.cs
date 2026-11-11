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
                    StartDialogue();
                }
            }
        }
    }

    private void StartDialogue()
    {
        DialogueInitData data = new DialogueInitData();
        data.lines = dialogueNPC.GetDialogueLines();
        data.npcId = dialogueNPC.NpcId;
        data.characterName = dialogueNPC.NpcName;
        data.voice = dialogueNPC.NpcVoice;
        data.portrait = dialogueNPC.Portrait;
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