using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public GameObject OptionMenu;
    public void PlayGame()
    {
        SceneManager.LoadScene(1);
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
