using UnityEngine;
using UnityEngine.EventSystems;

public class SelectedButtonSound : MonoBehaviour, ISelectHandler
{
    public AudioClip selectSound;
    public float volume = 0.3f;

    public void OnSelect(BaseEventData eventData)
    {
        if (ButtonSoundManager.IgnoreNextSelection)
        {
            ButtonSoundManager.IgnoreNextSelection = false;
            return;
        }
        SoundFXManager.instance.PlaySound(selectSound, transform, volume);
    }
}
