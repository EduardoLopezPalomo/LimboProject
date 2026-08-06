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
    public void SpawnRandomSFX(SFXType sfxType)
    {
        SFXDefinition[] filteredDefs = System.Array.FindAll(SFXDefinitions, def => def.type == sfxType);
        if (filteredDefs.Length > 0)
        {
            int randomIndex = Random.Range(0, filteredDefs.Length);
            SFXDefinition randomDef = filteredDefs[randomIndex];
            SoundFXManager.instance.PlaySoundRandomPitch(randomDef.sfx, transform, randomDef.volume, randomDef.minPitch, randomDef.maxPitch);
        }

    }
}
