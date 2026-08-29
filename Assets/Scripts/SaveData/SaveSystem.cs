using System;
using UnityEngine;
using System.IO;

public static class SaveSystem 
{
    private static string AudioPath => Path.Combine(Application.persistentDataPath, "audioData.json");
    private static string GamePath => Path.Combine(Application.persistentDataPath, "gameData.json");

    public static event Action<string> OnSaveGame;
    public static event Action<string> OnFailLoadGame;

    #region Audio
        
    public static void SaveAudioData(SoundMixerManager soundMixerManager)
    {
        AudioData data = new AudioData(soundMixerManager);
        string json = JsonUtility.ToJson(data, true);

        try
        {
            File.WriteAllText(AudioPath, json);
            OnSaveGame?.Invoke("Settings Saved");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving audio data: {e.Message}");
            OnSaveGame?.Invoke("Fail to save settings");
        }
    }

    public static AudioData LoadAudioData()
    {
        if (!File.Exists(AudioPath))
        {
            Debug.LogWarning("Save file not found in " + AudioPath);
            return null;
        }

        try
        {
            string json = File.ReadAllText(AudioPath);
            return JsonUtility.FromJson<AudioData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading audio data: {e.Message}");
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
