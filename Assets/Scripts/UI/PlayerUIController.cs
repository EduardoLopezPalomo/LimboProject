using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerUIController : MonoBehaviour
{
    public GameObject playerUI;
    public GameObject PauseMenu;
    public GameObject AblityMenu;
    public float TotalVolumeDB = -6f;
    public float DurationMusicFade = 2f;
    
    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        if (PauseMenu)
        {
            PauseMenu menu = PauseMenu.GetComponent<PauseMenu>();
            menu.player = gameObject;   
        }
    }

    public void PressStartButton()
    {
        if (playerUI == null || PauseMenu == null || AblityMenu == null) return;
        playerUI.SetActive(false);
        PauseMenu.SetActive(true);
        playerInput.SwitchCurrentActionMap("UI");
        TimeEffectsManager.PauseGame();

        SoundMixerManager.instance.FadeVolumeToTarget(MixerNames.pausedVolume, TotalVolumeDB, DurationMusicFade);
    }

    public void PressAbilityMenuButton()
    {
        if (playerUI == null || PauseMenu == null || AblityMenu == null) return;
        if (PauseMenu.activeSelf) return;
        
        if (AblityMenu.activeSelf)
        {
            playerUI.SetActive(true);
            AblityMenu.SetActive(false);
            playerInput.SwitchCurrentActionMap("Player");
            TimeEffectsManager.ResumeGame();

            SoundMixerManager.instance.FadeVolumeToTarget(MixerNames.pausedVolume, 0f, DurationMusicFade);
        }
        else
        {
            playerUI.SetActive(false);
            AblityMenu.SetActive(true);
            playerInput.SwitchCurrentActionMap("UI");
            TimeEffectsManager.PauseGame();

            SoundMixerManager.instance.FadeVolumeToTarget(MixerNames.pausedVolume, TotalVolumeDB, DurationMusicFade);   
        }
    }
    
}
