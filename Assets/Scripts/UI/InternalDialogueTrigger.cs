using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class InternalDialogueTrigger : MonoBehaviour, IConversation
{
    [Header("Contenido del diálogo interno")]
    [FormerlySerializedAs("lineas")]
    [SerializeField] private List<string> lines;

    [Header("Apariencia (protagonista)")]
    [FormerlySerializedAs("nombreProtagonista")]
    [SerializeField] private string protagonistName = "Tú";
    [FormerlySerializedAs("retratoProtagonista")]
    [SerializeField] private NpcPortrait protagonistPortrait;
    [FormerlySerializedAs("vozProtagonista")]
    [SerializeField] private VoiceType protagonistVoice = VoiceType.Masculine;

    [Header("Comportamiento")]
    [FormerlySerializedAs("soloUnaVez")]
    [SerializeField] private bool onceOnly = true;


    private bool alreadyShown = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (onceOnly && alreadyShown) return;

        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput == null) return;
        
        DialogueInitData data = new DialogueInitData();
        data.lines = lines;
        data.npcId = "";
        data.characterName = protagonistName;
        data.voice = protagonistVoice;
        data.portrait = protagonistPortrait;
        data.input = playerInput;
        
        IConversation.RaiseDialogueInit(data);
        
        playerInput.SwitchCurrentActionMap("UI");

        alreadyShown = true;
    }
}