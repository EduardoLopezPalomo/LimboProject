using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerUIController : MonoBehaviour
{
    public GameObject playerUI;
    public GameObject PauseMenu;
    public GameObject AblityMenu;
    public GameObject OptionsMenu;
    public GameObject DeathMenu;
    public float TotalVolumeDB = -6f;
    public float DurationMusicFade = 2f;

    private PlayerInput playerInput;
    private MenuNavigation optionsMenuNavigation;

    // T-013: SoundMixerManager is not on this object - it arrives on the GlobalManager prefab - so it is null in
    // any scene that does not carry that prefab, and during the frames before its Awake has run. This class used
    // to dereference it four times, which turned a pause keypress into a NullReferenceException in exactly the
    // menu the player is trying to reach. See FadePauseVolume for what happens instead.
    private bool warnedMissingMixer;

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

        if (OptionsMenu) optionsMenuNavigation = OptionsMenu.GetComponent<MenuNavigation>();
    }

    public void PressStartButton()
    {
        if (playerUI == null || PauseMenu == null || AblityMenu == null) return;
        playerUI.SetActive(false);
        PauseMenu.SetActive(true);
        playerInput.SwitchCurrentActionMap("UI");
        TimeEffectsManager.PauseGame();

        FadePauseVolume(TotalVolumeDB);
    }

    public void PressAbilityMenuButton()
    {
        if (playerUI == null || PauseMenu == null || AblityMenu == null || OptionsMenu == null) return;
        if (PauseMenu.activeSelf) return;

        playerUI.SetActive(false);
        AblityMenu.SetActive(true);
        playerInput.SwitchCurrentActionMap("UI");
        TimeEffectsManager.PauseGame();

        FadePauseVolume(TotalVolumeDB);

    }

    public void OnBackMenu()
    {
        if (AblityMenu.activeSelf)
        {
            playerUI.SetActive(true);
            AblityMenu.SetActive(false);
            playerInput.SwitchCurrentActionMap("Player");
            TimeEffectsManager.ResumeGame();

            FadePauseVolume(0f);
        }
        else if (PauseMenu.activeSelf)
        {
            playerUI.SetActive(true);
            PauseMenu.SetActive(false);
            playerInput.SwitchCurrentActionMap("Player");
            TimeEffectsManager.ResumeGame();

            FadePauseVolume(0f);
        }
        else if (OptionsMenu.activeSelf)
        {
            optionsMenuNavigation.GoBack();
        }
    }

    public void ShowDeathMenu()
    {
        DeathMenu.SetActive(true);
        playerInput.SwitchCurrentActionMap("UI");
    }

    /// <summary>
    /// T-013: the one place this class touches the audio mixer. The fade is cosmetic, so a scene without
    /// SoundMixerManager skips the fade and the menu transition continues - the old direct call threw instead, and
    /// because it sat last in each method it threw after the panel was already swapped, leaving the game paused
    /// with the UI half changed. Warned once (see warnedMissingMixer) rather than once per keypress.
    /// </summary>
    private void FadePauseVolume(float targetDb)
    {
        SoundMixerManager mixer = SoundMixerManager.instance;
        if (mixer == null)
        {
            if (!warnedMissingMixer)
            {
                warnedMissingMixer = true;
                Debug.LogWarning("PlayerUIController: no SoundMixerManager in this scene (it arrives on the "
                    + "GlobalManager prefab), so the pause music fade is skipped. The menus still open and close.",
                    this);
            }
            return;
        }

        mixer.FadeVolumeToTarget(MixerNames.pausedVolume, targetDb, DurationMusicFade);
    }

}
