using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class WorldData
{
    public int sceneIndex;
    
    public WorldData()
    {
        sceneIndex = SceneManager.GetActiveScene().buildIndex;
    }
}
