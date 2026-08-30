using UnityEngine;

[System.Serializable]
public class SettingsData
{
    public AudioData audioData;
    public GameplayData gameplayData;
    
    public SettingsData() { }

    public SettingsData(SoundMixerManager soundMixerManager)
    {
        audioData = new AudioData(soundMixerManager);
        gameplayData = new GameplayData();
    }
}
