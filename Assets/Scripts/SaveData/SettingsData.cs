using UnityEngine;

[System.Serializable]
public class SettingsData
{
    public AudioData audioData;
    public GameplayData gameplayData;
    public InterfaceData interfaceData;
    
    public SettingsData() { }

    public SettingsData(SoundMixerManager soundMixerManager, VibrationManager vibrationManager, InterfaceManager interfaceManager)
    {
        audioData = new AudioData(soundMixerManager);
        gameplayData = new GameplayData(vibrationManager);
        interfaceData = new InterfaceData(interfaceManager);
    }
}
