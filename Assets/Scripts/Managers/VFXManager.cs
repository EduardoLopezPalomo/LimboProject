using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    private Dictionary<VFXDefinition, Queue<GameObject>> pools = new();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    
    public GameObject Play(VFXDefinition def, Vector3 position, Quaternion rotation, GameObject target)
    {
        Quaternion finalRot = rotation * Quaternion.Euler(def.rotationOffset);
        GameObject vfx = def.useObjectPool ? GetFromPool(def) : Instantiate(def.prefab);
        
        if (def.useObjectPool)
            vfx.transform.SetParent(transform, worldPositionStays: false);

        vfx.transform.SetPositionAndRotation(position, finalRot);
        vfx.transform.localScale = def.scaleOverride;
        vfx.SetActive(true);

        if (def.useObjectPool)
        {
            StartCoroutine(ReturnToPoolAfter(def, vfx, def.lifetime));
        }
        else
            Destroy(vfx, def.lifetime);

        if (def.followTarget)
        {
            StartCoroutine(FollowTarget(vfx, target, def));
        }

        return vfx;
    }
    
    public GameObject Play(VFXDefinition def, Transform spawnPoint, GameObject target) => Play(def, spawnPoint.position, spawnPoint.rotation, target);

    private GameObject GetFromPool(VFXDefinition def)
    {
        if (!pools.TryGetValue(def, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[def] = queue;
        }

        if (queue.Count > 0)
        {
            var obj = queue.Dequeue();
            if (obj != null)
                return obj;
        }

        return Instantiate(def.prefab);
    }

    private System.Collections.IEnumerator ReturnToPoolAfter(VFXDefinition def, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
        pools[def].Enqueue(obj);
    }
    
    private System.Collections.IEnumerator FollowTarget(GameObject vfx, GameObject target, VFXDefinition def)
    {
        float elapsed = 0f;

        while (elapsed < def.lifetime)
        {
            if (vfx == null || target == null)
                yield break;

            vfx.transform.position = target.transform.position + def.positionOffset;
            vfx.transform.rotation = target.transform.rotation * Quaternion.Euler(def.rotationOffset);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
