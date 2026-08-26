using UnityEngine;

[System.Serializable]
public class GameData 
{
    public WorldData worldData;
    public PlayerData playerData;
    
    
    public GameData() { }

    public GameData(GameObject player)
    {
        worldData = new WorldData();
        playerData = new PlayerData(player);
    }
}
