using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerUIController : MonoBehaviour
{
    public GameObject playerUI;
    public GameObject PauseMenu;
    public float TotalVolumeDB = -6f;
    public float DurationMusicFade = 2f;
    
    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    public void PressStartButton()
    {
        playerUI.SetActive(false);
        PauseMenu.SetActive(true);
        playerInput.SwitchCurrentActionMap("UI");
        PauseGameManager.PauseGame();

        SoundMixerManager.instance.FadeVolumeToTarget(MixerNames.pausedVolume, TotalVolumeDB, DurationMusicFade);
    }
    
}
