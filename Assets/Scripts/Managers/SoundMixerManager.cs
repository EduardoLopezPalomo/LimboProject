using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public struct MixerNames
{
    public static string masterVolume = "MasterVolume";
    public static string musicVolume = "MusicVolume";
    public static string sfxVolume = "SFXVolume";
    public static string ambienceVolume = "AmbienceVolume";
    public static string pausedVolume = "PauseVolume";
}

public class SoundMixerManager : MonoBehaviour
{
    public static SoundMixerManager instance;
    [SerializeField] private AudioMixer audioMixer;
    
    private Coroutine currentFade;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        LoadAudioSettings();
    }

    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat(MixerNames.masterVolume, Mathf.Log10(volume) * 20);
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat(MixerNames.musicVolume, Mathf.Log10(volume) * 20);
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat(MixerNames.sfxVolume, Mathf.Log10(volume) * 20);
    }
    
    public void SetAmbienceVolume(float volume)
    {
        audioMixer.SetFloat(MixerNames.ambienceVolume, Mathf.Log10(volume) * 20);
    }

    public float GetMasterVolume()
    {
        return audioMixer.GetFloat(MixerNames.masterVolume, out float volume) ? volume : 0f;
    }
    
    public float GetMusicVolume()
    {
        return audioMixer.GetFloat(MixerNames.musicVolume, out float volume) ? volume : 0f;
    }
    
    public float GetSFXVVolume()
    {
        return audioMixer.GetFloat(MixerNames.sfxVolume, out float volume) ? volume : 0f;
    }
    
    public float GetAmbienceVolume()
    {
        return audioMixer.GetFloat(MixerNames.ambienceVolume, out float volume) ? volume : 0f;
    }
    
    public void FadeVolumeToTarget(string name, float targetDb, float duration)
    {
        if (currentFade != null)
            StopCoroutine(currentFade);
        
        currentFade = StartCoroutine(FadeVolume(name, targetDb ,duration));
    }

    public void ParallelFadeVolume(string name, float targetDb, float duration)
    {
        StartCoroutine(FadeVolume(name, targetDb, duration));
    }

    IEnumerator FadeVolume(string name, float targetDb, float duration)
    {
        audioMixer.GetFloat(name, out float startDb);

        float startLinear = Mathf.Pow(10, startDb / 20);
        float targetLinear = Mathf.Pow(10, targetDb / 20);

        float time = 0;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float linear = Mathf.Lerp(startLinear, targetLinear, time / duration);
            
            linear = Mathf.Max(linear, 0.0001f);

            audioMixer.SetFloat(name, Mathf.Log10(linear) * 20);

            yield return null;
        }

        audioMixer.SetFloat(name, targetDb);
    }
    
    public void LoadAudioSettings()
    {
        AudioData cachedData = SaveSystem.LoadAudioData();   
        
        if (cachedData != null)
        {
            StartCoroutine(ApplyAudioSettingsFrame1(cachedData.masterVolume, cachedData.musicVolume, cachedData.sfxVolume, cachedData.ambienceVolume));
        }
    }
    
    IEnumerator ApplyAudioSettingsFrame1(float masterVolume, float musicVolume, float sfxVolume, float ambienceVolume)
    {
        yield return null; 
        
        SetMasterVolume(DecibelToLinear(masterVolume));
        SetMusicVolume(DecibelToLinear(musicVolume));
        SetSFXVolume(DecibelToLinear(sfxVolume));
        SetAmbienceVolume(DecibelToLinear(ambienceVolume));
    }

    
    public void RevertToSaved()
    {
        LoadAudioSettings();
    }
    
    private float DecibelToLinear(float dB)
    {
        return Mathf.Pow(10, dB / 20);
    }
 
}
