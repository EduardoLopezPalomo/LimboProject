using UnityEngine;
using UnityEngine.UI;

public class ApplyAudioData : MonoBehaviour
{
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider SfxVolumeSlider;
    public Slider AmbienceVolumeSlider;

    void Start()
    {
        AudioData data = SaveSystem.LoadAudioData();
        if (data != null)
        {
            masterVolumeSlider.value = DecibelToLinear(data.masterVolume);
            musicVolumeSlider.value = DecibelToLinear(data.musicVolume);
            SfxVolumeSlider.value = DecibelToLinear(data.sfxVolume);
            AmbienceVolumeSlider.value = DecibelToLinear(data.ambienceVolume);   
        }
    }
    
    
    private float DecibelToLinear(float dB)
    {
        return Mathf.Pow(10, dB / 20);
    }
    
    
}
