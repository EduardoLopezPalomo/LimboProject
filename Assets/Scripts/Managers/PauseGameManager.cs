using UnityEngine;

public static class PauseGameManager
{
    private static bool isPaused = false;

    public static void PauseGame()
    {
        if(!isPaused)
        {
            Time.timeScale = 0f;
            isPaused = true;
        }
    }
    
    public static void ResumeGame()
    {
        if(isPaused)
        {
            Time.timeScale = 1f;
            isPaused = false;
        }
    }
}
