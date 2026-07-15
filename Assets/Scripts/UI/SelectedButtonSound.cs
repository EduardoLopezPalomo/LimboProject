using UnityEngine;
using UnityEngine.EventSystems;

public class SelectedButtonSound : MonoBehaviour, ISelectHandler
{
    public AudioClip selectSound;
    public float volume = 0.3f;

    public void OnSelect(BaseEventData eventData)
    {
        SoundFXManager.instance.PlaySound(selectSound, transform, volume);
    }
}
