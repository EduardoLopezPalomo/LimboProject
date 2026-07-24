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
                VFXManager.Instance.Play(def,vfxSpawnPoint.position, finalRotation);
            }
        }
    }
}
