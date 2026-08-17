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
                Vector3 offset = def.rotationOffset;
                Quaternion finalRotation = vfxSpawnPoint.rotation * Quaternion.Euler(offset);
                
                Vector3 positionOffset = def.positionOffset;
                Vector3 finalPosition = vfxSpawnPoint.position + positionOffset;
                
                VFXManager.Instance.Play(def,finalPosition, finalRotation, gameObject);
            }
        }
    }

    public (VFXDefinition def, GameObject instance) SpawnRandomVFX(VFXType vfxType)
    {
        VFXDefinition[] filteredDefs = System.Array.FindAll(VFXDefinitions, def => def.type == vfxType);
        if (filteredDefs.Length == 0) return(null, null);
        
        int randomIndex = Random.Range(0, filteredDefs.Length);
        
        VFXDefinition randomDef = filteredDefs[randomIndex];
        Vector3 offset = randomDef.rotationOffset;
        Quaternion finalRotation = vfxSpawnPoint.rotation * Quaternion.Euler(offset);
        
        Vector3 positionOffset = randomDef.positionOffset;
        Vector3 finalPosition = vfxSpawnPoint.position + positionOffset;
        
        GameObject instance = VFXManager.Instance.Play(randomDef,finalPosition, finalRotation, gameObject);
            
        return (randomDef, instance);
    }

    public void CancelVFX(GameObject instance)
    {
        VFXManager.Instance.Cancel(instance);
    }
}
