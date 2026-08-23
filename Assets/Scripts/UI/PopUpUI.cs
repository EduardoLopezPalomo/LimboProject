using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

public class PopUpUI : MonoBehaviour
{
    public TMP_Text infoDelPopUp;

    private bool PartyOn = false;
    private PlayerInput playerInput;
    private string sceneName;

    public void MostrarMensaje(string mensaje)
    {
        infoDelPopUp.text = mensaje;
    }

    public void SetVariables(bool partyOn, PlayerInput input, string scene)
    {
        PartyOn = partyOn;
        playerInput = input;
        sceneName = scene;
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
        playerInput.SwitchCurrentActionMap("Player");
        TimeEffectsManager.ResumeGame();
    }

    public void Cancelar()
    {
        gameObject.SetActive(false);

        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("Player");
        }

        TimeEffectsManager.ResumeGame();
    }
}
