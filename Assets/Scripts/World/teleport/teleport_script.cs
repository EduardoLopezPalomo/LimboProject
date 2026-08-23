using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class teleport_script : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private bool PartyOn = false;
    [SerializeField] private GameObject popUp;
    [SerializeField] private GameObject botonAceptar;

    [SerializeField][TextArea] private string mensajePopUP;


    private PlayerInput playerInput;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            popUp.SetActive(true);
            PopUpUI popUpUI = popUp.GetComponent<PopUpUI>();

            playerInput = other.GetComponent<PlayerInput>();

            if (playerInput != null && popUpUI != null)
            {
                playerInput.SwitchCurrentActionMap("UI");
                popUpUI.SetVariables(PartyOn, playerInput, sceneName);
                popUpUI.MostrarMensaje(mensajePopUP);
                
            }

            EventSystem.current.SetSelectedGameObject(botonAceptar);
            TimeEffectsManager.PauseGame();


        }
    }
}