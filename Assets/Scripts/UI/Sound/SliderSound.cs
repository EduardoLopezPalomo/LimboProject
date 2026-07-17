using UnityEngine;

public class SliderSound : MonoBehaviour
{
    [Header("Value Change Sound")]
    public AudioClip sound;
    public float volume = 0.3f;
    public float pitchMin = 0.9f;
    public float pitchMax = 1.1f;
    
    public void OnValueChange(float value)
    {
        SoundFXManager.instance.PlaySoundRandomPitch(sound, transform, volume, pitchMin, pitchMax);
    }
}
