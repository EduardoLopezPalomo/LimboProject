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
                
                VFXManager.Instance.Play(def,vfxSpawnPoint.position, vfxSpawnPoint.rotation, gameObject);
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
            
        return (randomDef, instance);
    }

    public void CancelVFX(GameObject instance)
    {
        VFXManager.Instance.Cancel(instance);
    }
}
