using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InternalDialogueTrigger : MonoBehaviour, IConversation
{
    [Header("Contenido del diálogo interno")]
    [SerializeField] private List<string> lineas;

    [Header("Apariencia (protagonista)")]
    [SerializeField] private string nombreProtagonista = "Tú";
    [SerializeField] private Sprite retratoProtagonista;
    [SerializeField] private TipoVoz vozProtagonista = TipoVoz.Masculina;

    [Header("Comportamiento")]
    [SerializeField] private bool soloUnaVez = true;


    private bool yaMostrado = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (soloUnaVez && yaMostrado) return;

        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput == null) return;
        
        DialogueInitData data = new DialogueInitData();
        data.lineas = lineas;
        data.npcId = "";
        data.nombre = nombreProtagonista;
        data.voz = vozProtagonista;
        data.retrato = retratoProtagonista;
        data.input = playerInput;
        
        IConversation.RaiseDialogueInit(data);
        
        playerInput.SwitchCurrentActionMap("UI");

        yaMostrado = true;
    }
}