using UnityEngine;

public class CharacterVFX : MonoBehaviour
{
    [SerializeField] private Transform vfxSpawnPoint;
    [SerializeField] private VFXDefinition[] VFXDefinitions;
    
    
    public void SpawnVFX(VFXType vfxType)
    {
        foreach (var def in VFXDefinitions)
        {
            if (def.type == vfxType)
            {
                
                GameObject vfx = VFXManager.Instance.Play(def,vfxSpawnPoint.position, vfxSpawnPoint.rotation, gameObject);

                // This component sits on the character, so the character is whoever caused the effect.
                // Done after Play because Play is what activates the object, and a pooled hazard clears
                // its instigator when it is switched off (see IHasInstigator).
                HazardInstigator.Assign(vfx, gameObject);
            }
        }
    }

    public (VFXDefinition def, GameObject instance) SpawnRandomVFX(VFXType vfxType)
    {
        VFXDefinition[] filteredDefs = System.Array.FindAll(VFXDefinitions, def => def.type == vfxType);
        if (filteredDefs.Length == 0) return(null, null);
        
        int randomIndex = Random.Range(0, filteredDefs.Length);
        
        VFXDefinition randomDef = filteredDefs[randomIndex];
        
        GameObject instance = VFXManager.Instance.Play(randomDef,vfxSpawnPoint.position , vfxSpawnPoint.rotation, gameObject);

        // See SpawnVFX: the character that owns this component is the instigator of what it just spawned.
        HazardInstigator.Assign(instance, gameObject);

        return (randomDef, instance);
    }

    public void CancelVFX(GameObject instance)
    {
        VFXManager.Instance.Cancel(instance);
    }
}
