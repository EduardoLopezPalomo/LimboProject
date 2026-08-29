using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class NpcPlayerDetection : MonoBehaviour , IConversation
{
    public static bool PossibleInteraction = false;
    
    public float detectionAngle = 0.6f;

    [SerializeField] private GameObject interactUI;
    [SerializeField] private GameObject npc;
    [SerializeField] private DialogueNPC dialogueNPC;
    
    private GameObject player;
    private PlayerInput playerInput;
    private bool playerDetected = false;
    private bool mirandoAlNPC = false;

    void Update()
    {
        if (playerDetected)
        {
            Vector3 direction = (npc.transform.position - player.transform.position).normalized;

            float dot = Vector3.Dot(player.transform.forward, direction);

            mirandoAlNPC = dot > detectionAngle;

            //Debug.Log("Dot: " + dot + " | Umbral: " + detectionAngle);

            interactUI.SetActive(mirandoAlNPC);

            PossibleInteraction = mirandoAlNPC;

            if (mirandoAlNPC && playerInput.currentActionMap.name != "UI" && playerInput != null)
            {
                InputAction interactAction = playerInput.currentActionMap.FindAction("Interact");

                //Debug.Log("Mirando al NPC. Action encontrada: " + (interactAction != null ? interactAction.name : "NULL")
                //+ " | Mapa activo: " + playerInput.currentActionMap.name);

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
            //Debug.Log("Player detectado. PlayerInput: " + (playerInput != null ? "OK" : "NULL"));
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
