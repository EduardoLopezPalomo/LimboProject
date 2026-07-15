using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class SoundMixerManager : MonoBehaviour
{
    public static SoundMixerManager instance;
    [SerializeField] private AudioMixer audioMixer;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
    }
    
    public void SetAmbienceVolume(float volume)
    {
        audioMixer.SetFloat("AmbienceVolume", Mathf.Log10(volume) * 20);
    }
    
    public void FadeOutVolume(string name, float duration)
    {
        StartCoroutine(FadeVolume(name, -80f , duration));
    }
    
    public void FadeInVolume(string name,float duration)
    {
        StartCoroutine(FadeVolume(name, 0f ,duration));
    }

    IEnumerator FadeVolume(string name, float targetDb, float duration)
    {
        audioMixer.GetFloat(name, out float startDb);

        float startLinear = Mathf.Pow(10, startDb / 20);
        float targetLinear = Mathf.Pow(10, targetDb / 20);

        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float linear = Mathf.Lerp(startLinear, targetLinear, time / duration);
            
            linear = Mathf.Max(linear, 0.0001f);

            audioMixer.SetFloat(name, Mathf.Log10(linear) * 20);

            yield return null;
        }

        audioMixer.SetFloat(name, targetDb);
    }
 
}
