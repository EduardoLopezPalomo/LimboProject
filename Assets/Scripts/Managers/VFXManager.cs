using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    private Dictionary<VFXDefinition, Queue<GameObject>> pools = new();
    
    private class ActiveVFX
    {
        public VFXDefinition def;
        public Coroutine returnCoroutine;
        public Coroutine followCoroutine;
    }

    private Dictionary<GameObject, ActiveVFX> activeVFX = new();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    
    public GameObject Play(VFXDefinition def, Vector3 position, Quaternion rotation, GameObject target)
    {
        Vector3 finalPosition = position + def.positionOffset;
        
        Quaternion finalRot = rotation * Quaternion.Euler(def.rotationOffset);
        GameObject vfx = def.useObjectPool ? GetFromPool(def) : Instantiate(def.prefab);
        
        if (def.useObjectPool)
            vfx.transform.SetParent(transform, worldPositionStays: false);

        vfx.transform.SetPositionAndRotation(finalPosition, finalRot);
        vfx.transform.localScale = def.scaleOverride;
        vfx.SetActive(true);
        
        var active = new ActiveVFX { def = def };
        activeVFX[vfx] = active;
        if (!def.untilCancel)
        {
            if (def.useObjectPool)
            {
                active.returnCoroutine = StartCoroutine(ReturnToPoolAfter(def, vfx, def.lifetime));
            }
            else
                Destroy(vfx, def.lifetime);   
        }

        if (def.followTarget)
        {
            active.followCoroutine = StartCoroutine(FollowTarget(vfx, target, def));
        }

        return vfx;
    }
    
    public GameObject Play(VFXDefinition def, Transform spawnPoint, GameObject target) => Play(def, spawnPoint.position, spawnPoint.rotation, target);
    
    public void Cancel(GameObject vfx)
    {
        if (vfx == null) return;

        if (activeVFX.TryGetValue(vfx, out var active))
        {
            if (active.returnCoroutine != null) StopCoroutine(active.returnCoroutine);
            if (active.followCoroutine != null) StopCoroutine(active.followCoroutine);
            activeVFX.Remove(vfx);

            if (active.def.useObjectPool)
            {
                vfx.SetActive(false);
                vfx.transform.SetParent(transform, worldPositionStays: false);
                pools[active.def].Enqueue(vfx);
            }
            else
            {
                Destroy(vfx);
            }
        }
        else
        {
            Destroy(vfx);
        }
    }

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
        activeVFX.Remove(obj);
    }
    
    private System.Collections.IEnumerator FollowTarget(GameObject vfx, GameObject target, VFXDefinition def)
    {
        float elapsed = 0f;
        
        while (def.untilCancel || elapsed < def.lifetime)
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
