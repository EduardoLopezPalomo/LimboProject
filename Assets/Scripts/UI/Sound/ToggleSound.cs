using UnityEngine;
using UnityEngine.EventSystems;

public class ToggleSound : MonoBehaviour
{
    [Header("True Sound")]
    public AudioClip trueSound;
    public float volumeTrue = 0.3f;
    public float pitchTrueMin = 0.9f;
    public float pitchTrueMax = 1.1f;
    
    [Header("False Sound")]
    public AudioClip falseSound;
    public float volumeFalse = 0.3f;
    public float pitchFalseMin = 0.9f;
    public float pitchFalseMax = 1.1f;

    public void OnValueChange(bool value)
    {
        if (value)
        {
            SoundFXManager.instance.PlaySoundRandomPitch(trueSound, transform, volumeTrue, pitchTrueMin, pitchTrueMax);
        }
        else
        {
            SoundFXManager.instance.PlaySoundRandomPitch(falseSound, transform, volumeFalse, pitchFalseMin, pitchFalseMax);
        }
    }
  
}
