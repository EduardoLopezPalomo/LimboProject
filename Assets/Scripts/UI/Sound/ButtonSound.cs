using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonSound : MonoBehaviour, ISelectHandler
{
    [Header("Selected Sound")]
    public AudioClip selectSound;
    public float volumeSelected = 0.3f;
    public float pitchSelectedMin = 0.9f;
    public float pitchSelectedMax = 1.1f;
    
    [Header("CLick Sound")]
    public AudioClip clickSound;
    public float volumeClick = 0.3f;
    public float pitchClickMin = 0.9f;
    public float pitchClickMax = 1.1f;

    public void OnSelect(BaseEventData eventData)
    {
        if (ButtonSoundManager.IgnoreNextSelection)
        {
            ButtonSoundManager.IgnoreNextSelection = false;
            return;
        }
        SoundFXManager.instance.PlaySoundRandomPitch(selectSound, transform, volumeSelected, pitchSelectedMin, pitchSelectedMax);
    }

    public void OnClick()
    {
        SoundFXManager.instance.PlaySoundRandomPitch(clickSound, transform, volumeClick, pitchClickMin, pitchClickMax);
    }
}
