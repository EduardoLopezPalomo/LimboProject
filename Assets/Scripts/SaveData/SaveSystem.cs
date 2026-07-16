using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem 
{
    public static void SaveAudioData(SoundMixerManager soundMixerManager)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/audioData.save";
        FileStream stream = new FileStream(path, FileMode.Create);

        AudioData data = new AudioData(soundMixerManager);

        formatter.Serialize(stream, data);
        stream.Close();
    }

    public static AudioData LoadAudioData()
    {
        string path = Application.persistentDataPath + "/audioData.save";
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);
            
            AudioData data = formatter.Deserialize(stream) as AudioData;
            stream.Close();
            
            return data;
        }
        else
        {
            Debug.LogError("Save file not found in " + path);
            return null;
        }
    }
}
