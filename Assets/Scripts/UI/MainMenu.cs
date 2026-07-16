using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public GameObject OptionMenu;
    public LevelLoader levelLoader;
    public int levelToLoad = 1;

    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
    }

    public void PlayGame()
    {
        levelLoader.LoadLevel(levelToLoad);
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
    }

    public void Exit()
    {
        Application.Quit();
    }
}
