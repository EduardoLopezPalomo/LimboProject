using System;
using UnityEngine;
using UnityEngine.UI;

public class ApplyAudioData : MonoBehaviour
{
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider SfxVolumeSlider;
    public Slider AmbienceVolumeSlider;

    private void OnEnable()
    {
        SettingsData settingsData = SaveSystem.LoadSettingsData();
        if (settingsData == null) return;
        
        AudioData data = settingsData.audioData;
        if (data != null)
        {
            RefreshSliders(data);
        }
    }
    
    public void RefreshSliders(AudioData data)
    {
        masterVolumeSlider.SetValueWithoutNotify(DecibelToLinear(data.masterVolume));
        musicVolumeSlider.SetValueWithoutNotify(DecibelToLinear(data.musicVolume));
        SfxVolumeSlider.SetValueWithoutNotify(DecibelToLinear(data.sfxVolume));
        AmbienceVolumeSlider.SetValueWithoutNotify(DecibelToLinear(data.ambienceVolume));
    }
    
    
    private float DecibelToLinear(float dB)
    {
        return Mathf.Pow(10, dB / 20);
    }
    
    
}
