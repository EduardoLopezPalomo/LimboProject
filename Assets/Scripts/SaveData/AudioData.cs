using UnityEngine;

[System.Serializable]
public class AudioData 
{
    public float masterVolume;
    public float musicVolume;
    public float sfxVolume;
    public float ambienceVolume;

    public AudioData(SoundMixerManager soundMixerManager)
    {
        masterVolume = soundMixerManager.GetMasterVolume();
        musicVolume = soundMixerManager.GetMusicVolume();
        sfxVolume = soundMixerManager.GetSFXVVolume();
        ambienceVolume = soundMixerManager.GetAmbienceVolume();
    }
}
