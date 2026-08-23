using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public GameObject OptionMenu;
    public GameObject HUD;
    public LevelLoader levelLoader;
    public Button continueButton;
    public int levelToLoad = 0;
    public float DurationMusicFade = 2f;

    private Canvas canvas;
    private ChangeActionMap changeActionMap;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        changeActionMap = GetComponent<ChangeActionMap>();
    }

    private void OnEnable()
    {
        continueButton.Select();
    }

    public void Continue()
    {
        gameObject.SetActive(false);
        HUD.SetActive(true);
        changeActionMap.ChangeToPlayer();
        TimeEffectsManager.ResumeGame();
        SoundMixerManager.instance.FadeVolumeToTarget(MixerNames.pausedVolume, 0f, DurationMusicFade);

    }

    public void Options()
    {
        OptionMenu.SetActive(true);
        gameObject.SetActive(false);
    }

    public void Save()
    {
        Debug.Log("Guardando");
    }

    public void MainMenu()
    {
        TimeEffectsManager.ResumeGame();
        SoundMixerManager.instance.FadeVolumeToTarget(MixerNames.pausedVolume, 0f, DurationMusicFade);
        levelLoader.LoadLevel(levelToLoad);
        canvas.sortingOrder = -1;
        DayNight.Instance.CanChangePhase = false;
    }
}
