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

    // TODO (pendiente de la misión principal): aquí más adelante añadiremos
    // una comprobación tipo "if (!QuestManager.CondicionParaEsteDialogo()) return;"
    // antes de disparar el diálogo, para que solo aparezca cuando corresponda al flujo de misión.

    private bool yaMostrado = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (soloUnaVez && yaMostrado) return;
        if (dialogueUI.EnDialogo) return; // evita solaparse si ya hay un diálogo abierto

        PlayerInput playerInput = other.GetComponent<PlayerInput>();
        if (playerInput == null) return;

        dialogueUI.MostrarDialogo(lineas, nombreProtagonista, vozProtagonista, retratoProtagonista, playerInput);
        playerInput.SwitchCurrentActionMap("UI");

        yaMostrado = true;
    }
}