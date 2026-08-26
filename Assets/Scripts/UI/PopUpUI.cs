using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

public class PopUpUI : MonoBehaviour
{
    public TMP_Text infoDelPopUp;

    private bool PartyOn = false;
    private PlayerInput playerInput;
    private int sceneIndex;

    public void MostrarMensaje(string mensaje)
    {
        infoDelPopUp.text = mensaje;
    }

    public void SetVariables(bool partyOn, PlayerInput input, string scene)
    {
        PartyOn = partyOn;
        playerInput = input;
        sceneIndex = SceneUtility.GetBuildIndexByScenePath(scene);
    }
    
    public void Confirmar()
    {
        if (PartyOn)
        {
            int index = SceneUtility.GetBuildIndexByScenePath("Party");
            LevelLoader.Instance.LoadLevel(index);
        }
        else
        {
            LevelLoader.Instance.LoadLevel(sceneIndex);
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
