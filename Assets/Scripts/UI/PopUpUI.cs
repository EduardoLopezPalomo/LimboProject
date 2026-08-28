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
    private int sceneIndex;

    public void MostrarMensaje(string mensaje)
    {
        infoDelPopUp.text = mensaje;
    }

    public void SetVariables(bool partyOn, PlayerInput input, string scene, Vector3 point, Quaternion rotation)
    {
        PartyOn = partyOn;
        playerInput = input;
        sceneIndex = SceneUtility.GetBuildIndexByScenePath(scene);
        pointNewScene = point;
        rotationNewScene = rotation;
    }
    
    public void Confirmar()
    {
        if (PartyOn)
        {
            int index = SceneUtility.GetBuildIndexByScenePath("Party");
            LevelLoader.Instance.LoadLevelAndTeleportAsync(index, pointNewScene, rotationNewScene);
        }
        else
        {
            LevelLoader.Instance.LoadLevelAndTeleportAsync(sceneIndex, pointNewScene, rotationNewScene);
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
