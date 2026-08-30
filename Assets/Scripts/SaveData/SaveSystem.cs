using System;
using UnityEngine;
using System.IO;

public static class SaveSystem 
{
    private static string SettingsPath => Path.Combine(Application.persistentDataPath, "settingsData.json");
    private static string GamePath => Path.Combine(Application.persistentDataPath, "gameData.json");

    public static event Action<string> OnSaveGame;
    public static event Action<string> OnFailLoadGame;

    #region Audio
        
    public static void SaveSettingsData(SoundMixerManager soundMixerManager)
    {
        SettingsData data = new SettingsData(soundMixerManager);
        string json = JsonUtility.ToJson(data, true);

        try
        {
            File.WriteAllText(SettingsPath, json);
            OnSaveGame?.Invoke("Settings Saved");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving settings data: {e.Message}");
            OnSaveGame?.Invoke("Fail to save settings");
        }
    }

    public static SettingsData LoadSettingsData()
    {
        if (!File.Exists(SettingsPath))
        {
            Debug.LogWarning("Save file not found in " + SettingsPath);
            return null;
        }

        try
        {
            string json = File.ReadAllText(SettingsPath);
            return JsonUtility.FromJson<SettingsData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading settings data: {e.Message}");
            return null;
        }
    }
    

    #endregion

    #region Game

    public static void SaveGame(GameObject player)
    {
        GameData data = new GameData(player);
        string json = JsonUtility.ToJson(data, true);

        try
        {
            File.WriteAllText(GamePath, json);
            OnSaveGame?.Invoke("Game Saved");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving game: {e.Message}");
            OnSaveGame?.Invoke("Fail to save game");
        }
    }

    public static GameData LoadGame()
    {
        if (!File.Exists(GamePath))
        {
            Debug.LogWarning("Save file not found in " + GamePath);
            OnFailLoadGame?.Invoke("No save data found.");
            return null;
        }

        try
        {
            string json = File.ReadAllText(GamePath);
            GameData data = JsonUtility.FromJson<GameData>(json);

            if (!IsValidGameData(data))
            {
                Debug.LogError("Save file is corrupted or incomplete.");
                OnFailLoadGame?.Invoke("Save file is corrupted or incomplete.");
                return null;
            }
            
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error Loading game: {e.Message}");
            OnFailLoadGame?.Invoke("Failed to load save data.");
            return null;
        }
    }
    
    private static bool IsValidGameData(GameData data)
    {
        if (data == null) return false;
        if (data.worldData == null) return false;
        if (data.playerData == null) return false;
        if (data.playerData.position == null || data.playerData.position.Length < 3) return false;

        return true;
    }

    #endregion
}
