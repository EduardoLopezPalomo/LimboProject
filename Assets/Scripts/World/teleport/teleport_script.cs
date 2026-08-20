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

    private PlayerInput playerInput;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            popUp.SetActive(true);

            playerInput = other.GetComponent<PlayerInput>();

            if (playerInput != null)
            {
                playerInput.SwitchCurrentActionMap("UI");
            }

            EventSystem.current.SetSelectedGameObject(botonAceptar);
            PauseGameManager.PauseGame();


        }
    }

    public void Confirmar()
    {
        if (PartyOn)
        {
            SceneManager.LoadScene("Party");
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    public void Cancelar()
    {
        popUp.SetActive(false);

        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("Player");
        }

        PauseGameManager.ResumeGame();
    }
}