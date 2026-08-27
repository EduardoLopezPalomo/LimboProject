using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InternalDialogueTrigger : MonoBehaviour
{
    [Header("Contenido del diálogo interno")]
    [SerializeField] private List<string> lineas;

    [Header("Apariencia (protagonista)")]
    [SerializeField] private string nombreProtagonista = "Tú";
    [SerializeField] private Sprite retratoProtagonista;
    [SerializeField] private TipoVoz vozProtagonista = TipoVoz.Masculina;

    [Header("Referencias")]
    [SerializeField] private DialogueUI dialogueUI;

    [Header("Comportamiento")]
    [SerializeField] private bool soloUnaVez = true;

    [Header("Mision")]
    [SerializeField] private EstadoMision estadoAlMostrarse = EstadoMision.Inicio;


    private bool yaMostrado = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (soloUnaVez && yaMostrado) return;
        if (dialogueUI.EnDialogo) return; 

        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput == null) return;

        dialogueUI.MostrarDialogo(lineas, nombreProtagonista, vozProtagonista, retratoProtagonista, playerInput);
        playerInput.SwitchCurrentActionMap("UI");

        yaMostrado = true;

        if (estadoAlMostrarse != EstadoMision.Inicio) 
            QuestManager.Instance.AvanzarA(estadoAlMostrarse);
    }
}