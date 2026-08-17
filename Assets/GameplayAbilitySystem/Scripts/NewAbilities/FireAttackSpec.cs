using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FireAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private RotateAlongPlayer rotateAlongPlayer;
    
    private LayerMask groundLayer;
    private Transform player;
    private VFXDefinition firePrefab;
    private VFXDefinition positionPreview;
    private float timeBeforeExplosion;
    private int ExplosionsNumber;
    private float MaxRadiusFromPlayer;
    private float MinDistanceBetweenEach;
    
    private List<Vector3> positions = new List<Vector3>();
    

    public FireAttackSpec(GameplayAbility ability, GameObject owner, VFXDefinition firePrefab, VFXDefinition positionPrev,float time , int explosions, float radius, float distanceEach ,LayerMask mask) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        rotateAlongPlayer = owner.GetComponent<RotateAlongPlayer>();
        
        this.firePrefab = firePrefab;
        groundLayer = mask;
        positionPreview = positionPrev;
        timeBeforeExplosion = time;
        ExplosionsNumber = explosions;
        MaxRadiusFromPlayer = radius;
        MinDistanceBetweenEach = distanceEach;

        player = rotateAlongPlayer.player;

    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();

        positions = GetRandomGroundPositions(player.position, MaxRadiusFromPlayer, ExplosionsNumber - 1, MinDistanceBetweenEach);

        foreach (Vector3 position in positions)
        {
            SpawnVfx(position, positionPreview);
        }

        rotateAlongPlayer.StartCoroutine(WaitTillExplosion(timeBeforeExplosion));
        
        animator.SetTrigger("Attack"); 
    }

    public override void CancelAbility()
    {
        base.CancelAbility();
        positions.Clear();
    }

    public override void EndAbility()
    {
        base.EndAbility();
        positions.Clear();
    }
    
    private Vector3 TryGetRandomGroundPosition(Vector3 center, float radius)
    {
        Vector2 randomCircle = Random.insideUnitCircle * radius;
        Vector3 randomXZ = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

        Vector3 rayOrigin = randomXZ + Vector3.up * 10f; 

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100f, groundLayer))
        {
            return hit.point;
        }
        
        return center;
    }
    
    private List<Vector3> GetRandomGroundPositions(Vector3 center, float radius, int count, float minDistance)
    {
        List<Vector3> results = new List<Vector3>();
        results.Add( center + new Vector3(0, 0.1f, 0));
        
        int maxAttemptsPerPoint = 30;

        for (int i = 0; i < count; i++)
        {
            bool found = false;

            for (int attempt = 0; attempt < maxAttemptsPerPoint; attempt++)
            {
                Vector3 candidate = TryGetRandomGroundPosition(center, radius);

                if (IsFarEnough(candidate, results, minDistance))
                {
                    results.Add(candidate);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.Log("There is no valid point");
            }
        }

        return results;
    }

    private bool IsFarEnough(Vector3 candidate, List<Vector3> existingPoints, float minDistance)
    {
        foreach (Vector3 point in existingPoints)
        {
            if (Vector3.Distance(candidate, point) < minDistance)
                return false;
        }
        return true;
    }

    private void SpawnVfx(Vector3 position, VFXDefinition def)
    {
        VFXManager.Instance.Play(def, position, Quaternion.identity, null);
    }


    private IEnumerator WaitTillExplosion(float time)
    {
        yield return new WaitForSeconds(time);

        foreach (Vector3 position in positions)
        {
            SpawnVfx(position, firePrefab);
        }
        EndAbility();
    }

}
