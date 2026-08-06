using UnityEngine;

public class CharacterSFX : MonoBehaviour
{
    [SerializeField] private SFXDefinition[] SFXDefinitions;


    public void SpawnSFX(SFXType sfxType)
    {
        foreach (var def in SFXDefinitions)
        {
            if (def.type == sfxType)
            {
                SoundFXManager.instance.PlaySoundRandomPitch(def.sfx, transform, def.volume, def.minPitch, def.maxPitch);
            }
        }
    }
}
