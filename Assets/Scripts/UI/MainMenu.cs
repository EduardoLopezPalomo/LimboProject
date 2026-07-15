using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public GameObject OptionMenu;
    public LevelLoader levelLoader;
    
    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
    }

    public void PlayGame()
    {
        levelLoader.LoadLevel();
        canvas.sortingOrder = -1;
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
