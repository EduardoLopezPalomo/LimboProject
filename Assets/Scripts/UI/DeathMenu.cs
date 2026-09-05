using UnityEngine;
using UnityEngine.UI;

public class DeathMenu : MonoBehaviour
{
    public Button continueButton;

    private ChangeActionMap changeActionMap;
    private Canvas canvas;
    private GameLoader gameLoader;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        changeActionMap = GetComponent<ChangeActionMap>();
        gameLoader = GetComponent<GameLoader>();
    }

    private void OnEnable()
    {
        continueButton.Select();
        TimeEffectsManager.PauseGame();
        canvas.sortingOrder = 2;
    }

    public void ReStartGame()
    {
        TimeEffectsManager.ResumeGame();
        gameLoader.LoadSaveGame();
        gameObject.SetActive(false);
        changeActionMap.ChangeToPlayer();
    }

    public void MainMenu()
    {
        TimeEffectsManager.ResumeGame();
        LevelLoader.Instance.LoadLevelAsync("Main Menu");
        canvas.sortingOrder = -1;
        DayNight.Instance.CanChangePhase = false;
    }

}
