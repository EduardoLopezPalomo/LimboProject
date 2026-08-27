using UnityEngine;
using UnityEngine.InputSystem;

public class QuestItemInteractable : MonoBehaviour
{
    [Header("Condición para poder interactuar")]
    [SerializeField] private EstadoMision estadoRequerido = EstadoMision.Inicio;

    [Header("Efecto al interactuar")]
    [SerializeField] private string flagAEstablecer;
    [SerializeField] private EstadoMision estadoAlCompletar = EstadoMision.Inicio;
    [SerializeField] private bool ocultarTrasInteractuar = true;

    [Header("Referencias")]
    [SerializeField] private GameObject interactUI;

    private bool jugadorDentro = false;
    private PlayerInput playerInput;

    private void Update()
    {
        if (!jugadorDentro || playerInput == null || !PuedeInteractuar()) return;

        InputAction interactAction = playerInput.currentActionMap.FindAction("Interact");
        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            Interactuar();
        }
    }

    private bool PuedeInteractuar()
    {
        return estadoRequerido == EstadoMision.Inicio || QuestManager.Instance.EstaEnEstado(estadoRequerido);
    }

    private void Interactuar()
    {
        if (!string.IsNullOrEmpty(flagAEstablecer))
            QuestManager.Instance.MarcarFlag(flagAEstablecer);

        if (estadoAlCompletar != EstadoMision.Inicio)
            QuestManager.Instance.AvanzarA(estadoAlCompletar);

        if (interactUI != null) interactUI.SetActive(false);
        if (ocultarTrasInteractuar) gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        jugadorDentro = true;
        playerInput = other.GetComponent<PlayerInput>();
        if (interactUI != null && PuedeInteractuar()) interactUI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        jugadorDentro = false;
        if (interactUI != null) interactUI.SetActive(false);
    }
}