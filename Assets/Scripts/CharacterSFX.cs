using System;
using UnityEngine;

[Serializable]
public struct SFX
{
    public AudioClip sfx;
    public float volume;
    public float minPitch;
    public float maxPitch;
}

public class CharacterSFX : MonoBehaviour
{

    public void StartSFX(SFX[] SFXes)
    {
        if (SFXes == null || SFXes.Length == 0) return;
        int random = UnityEngine.Random.Range(0, SFXes.Length);

        SoundFXManager.instance.PlaySoundRandomPitch(SFXes[random].sfx, transform, SFXes[random].volume, SFXes[random].minPitch, SFXes[random].maxPitch);
    }

}
