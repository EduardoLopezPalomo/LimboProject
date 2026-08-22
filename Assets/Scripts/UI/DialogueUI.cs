using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject panelDialogo;
    [SerializeField] private TMP_Text textoDialogo;
    [SerializeField] private TMP_Text nombreNPC;

    private Queue<string> colaLineas = new Queue<string>();
    private PlayerInput playerInputActual;

    public bool EnDialogo => panelDialogo.activeSelf;

    private void Update()
    {
        if (!EnDialogo || playerInputActual == null) return;

        InputAction interactAction = playerInputActual.currentActionMap.FindAction("Interact");
        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            MostrarSiguienteLinea();
        }
    }

    public void MostrarDialogo(List<string> lineas, string nombre, PlayerInput input = null)
    {

        Debug.Log("MostrarDialogo llamado con " + lineas.Count + " líneas"); colaLineas = new Queue<string>(lineas);
        playerInputActual = input;
        nombreNPC.text = nombre;
        panelDialogo.SetActive(true);
        MostrarSiguienteLinea();
    }

    public void MostrarSiguienteLinea()
    {
        if (colaLineas.Count == 0)
        {
            CerrarDialogo();
            return;
        }

        textoDialogo.text = colaLineas.Dequeue();
    }

    private void CerrarDialogo()
    {
        panelDialogo.SetActive(false);

        if (playerInputActual != null)
            playerInputActual.SwitchCurrentActionMap("Player");
    }
}