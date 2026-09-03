using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

public class PopUpUI : MonoBehaviour
{
    public TMP_Text infoDelPopUp;

    private bool PartyOn = false;
    private Vector3 pointNewScene;
    private Quaternion rotationNewScene;
    private PlayerInput playerInput;

    private string sceneName;

    public void MostrarMensaje(string mensaje)
    {
        infoDelPopUp.text = mensaje;
    }

    public void SetVariables(bool partyOn, PlayerInput input, string scene, Vector3 point, Quaternion rotation)
    {
        PartyOn = partyOn;
        playerInput = input;
        sceneName = scene;
        pointNewScene = point;
        rotationNewScene = rotation;
    }

    public void Confirmar()
    {
        if (PartyOn)
        {
            LevelLoader.Instance.LoadLevelAndTeleportAsync("Party", pointNewScene, rotationNewScene);
        }
        else
        {
            LevelLoader.Instance.LoadLevelAndTeleportAsync(sceneName, pointNewScene, rotationNewScene);
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
