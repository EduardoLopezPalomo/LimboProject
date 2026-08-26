using UnityEngine;
using System.IO;

public static class SaveSystem 
{
    private static string AudioPath => Path.Combine(Application.persistentDataPath, "audioData.json");
    private static string GamePath => Path.Combine(Application.persistentDataPath, "gameData.json");

    #region Audio
        
    public static void SaveAudioData(SoundMixerManager soundMixerManager)
    {
        AudioData data = new AudioData(soundMixerManager);
        string json = JsonUtility.ToJson(data, true);

        try
        {
            File.WriteAllText(AudioPath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving audio data: {e.Message}");
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
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving game: {e.Message}");
        }
    }

    public static GameData LoadGame()
    {
        if (!File.Exists(GamePath))
        {
            Debug.LogWarning("Save file not found in " + GamePath);
            return null;
        }

        try
        {
            string json = File.ReadAllText(GamePath);
            return JsonUtility.FromJson<GameData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error Loading game: {e.Message}");
            return null;
        }
    }

    #endregion
}
