using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PopUpUI : MonoBehaviour
{
    [FormerlySerializedAs("infoDelPopUp")]
    public TMP_Text messageText;

    private bool PartyOn = false;
    private Vector3 pointNewScene;
    private Quaternion rotationNewScene;
    private PlayerInput playerInput;

    private string sceneName;

    public void ShowMessage(string message)
    {
        messageText.text = message;
    }

    public void SetVariables(bool partyOn, PlayerInput input, string scene, Vector3 point, Quaternion rotation)
    {
        PartyOn = partyOn;
        playerInput = input;
        sceneName = scene;
        pointNewScene = point;
        rotationNewScene = rotation;
    }

    public void Confirm()
    {
        if (PartyOn)
        {
            LevelLoader.Instance.LoadLevelAndTeleportAsync("Party", pointNewScene, rotationNewScene, true);
        }
        else
        {
            LevelLoader.Instance.LoadLevelAndTeleportAsync(sceneName, pointNewScene, rotationNewScene, true);
        }
        playerInput.SwitchCurrentActionMap("Player");
        TimeEffectsManager.ResumeGame();
    }

    public void Cancel()
    {
        gameObject.SetActive(false);

        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("Player");
        }

        TimeEffectsManager.ResumeGame();
    }
}
