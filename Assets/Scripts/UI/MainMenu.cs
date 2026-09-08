using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public GameObject OptionMenu;
    public string levelToLoad = "Video_scene";

    private Canvas canvas;
    private GameLoader gameLoader;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        gameLoader = GetComponent<GameLoader>();
    }

    public void PlayGame()
    {
        PlayerAttributeCache.Clear();
        PlayerAbilitiesCache.Clear();
        QuestManager.instance.ReStart();
        LevelLoader.Instance.LoadLevelAsync(levelToLoad, null, false);
        canvas.sortingOrder = -1;
    }

    public void LoadSaveGame()
    {
        gameLoader.LoadSaveGame();
        canvas.sortingOrder = -1;
        DayNight.Instance.CanChangePhase = true;
    }

    public void Options()
    {
        OptionMenu.SetActive(true);
        gameObject.SetActive(false);
    }

    public void Credits()
    {
        Debug.Log("Créditos");
        LevelLoader.Instance.LoadLevelAsync("CreditsScene");
    }

    public void Exit()
    {
        Application.Quit();
    }
}
