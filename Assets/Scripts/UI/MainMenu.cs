using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene(1);
    }

    public void Options()
    {
        Debug.Log("Opciones");
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
