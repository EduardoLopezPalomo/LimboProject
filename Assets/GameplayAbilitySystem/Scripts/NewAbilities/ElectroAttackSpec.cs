using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ElectroAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private RotateAlongPlayer rotateAlongPlayer;
    
    private LayerMask groundLayer;
    private Transform player;
    private VFXDefinition Lightning;
    private VFXDefinition positionPreview;
    private float AttacksOfsetTime;
    private float TimeBeforeLighting;
    private int NumberOfLightings;
    private int MaxLightingPerColum;
    private float StartRadiusFromPlayer;
    private float MinDistanceBetweenEach;
    

    public ElectroAttackSpec(GameplayAbility ability, GameObject owner, VFXDefinition lightning, VFXDefinition positionPrev,float time, float attackDelay,
        int lightAttacks , int numberPerAttack, float radius, float distanceEach ,LayerMask mask) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        rotateAlongPlayer = owner.GetComponent<RotateAlongPlayer>();
        
        Lightning = lightning;
        groundLayer = mask;
        positionPreview = positionPrev;
        AttacksOfsetTime = attackDelay;
        TimeBeforeLighting = time;
        NumberOfLightings = lightAttacks;
        MaxLightingPerColum = numberPerAttack;
        StartRadiusFromPlayer = radius;
        MinDistanceBetweenEach = distanceEach;

        player = rotateAlongPlayer.player;

    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();

        rotateAlongPlayer.StartCoroutine(Attack(AttacksOfsetTime));
        
        animator.SetTrigger("Attack"); 
    }

    public override void CancelAbility()
    {
        base.CancelAbility();
        
    }

    public override void EndAbility()
    {
        base.EndAbility();
    }
    
    private Vector3 GetPointOnRadiusEdge(Vector3 center, float radius, float angleDegrees)
    {
        float angleRad = angleDegrees * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad)) * radius;
        Vector3 pointOnEdge = center + offset;

        return SnapToGround(pointOnEdge);
    }
    
    private List<Vector3> GetLineOfPointsThroughCenter(Vector3 edgePoint, Vector3 center, int pointCount, float spacing)
    {
        List<Vector3> results = new List<Vector3>();
        
        Vector3 direction = (center - edgePoint).normalized;

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 rawPoint = edgePoint + direction * (spacing * i);
            results.Add(SnapToGround(rawPoint));
        }

        return results;
    }
    
    private Vector3 SnapToGround(Vector3 point)
    {
        Vector3 rayOrigin = point + Vector3.up * 10f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100f, groundLayer))
        {
            return hit.point;
        }

        return point;
    }

    private void SpawnVfx(Vector3 position, VFXDefinition def)
    {
        VFXManager.Instance.Play(def, position, Quaternion.identity, null);
    }


    private IEnumerator ShowPreview(float delayTime, List<Vector3> pointList)
    {
        for (int i = 0; i < pointList.Count; i++)
        {
            SpawnVfx(pointList[i], positionPreview);

            if (i < pointList.Count - 1)
                yield return new WaitForSeconds(delayTime);
        }
    }
    
    private IEnumerator ShowLightnings(float delayTime, List<Vector3> pointList)
    {
        for (int i = 0; i < pointList.Count; i++)
        {
            SpawnVfx(pointList[i], Lightning);

            if (i < pointList.Count - 1)
                yield return new WaitForSeconds(delayTime);
        }
    }

    private IEnumerator Attack(float delayAttack)
    {
        for (int i = 0; i < NumberOfLightings; i++)
        {
            float angle = Random.Range(0f, 360f);
            Vector3 StartPoint = GetPointOnRadiusEdge(player.position, StartRadiusFromPlayer, angle);
            List<Vector3> AttacksPoints = GetLineOfPointsThroughCenter(StartPoint, player.position, MaxLightingPerColum, MinDistanceBetweenEach);
            
            rotateAlongPlayer.StartCoroutine(ShowPreview( 0.1f, AttacksPoints));
            yield return new WaitForSeconds(TimeBeforeLighting);
            rotateAlongPlayer.StartCoroutine(ShowLightnings(0.1f, AttacksPoints));
            yield return new WaitForSeconds(delayAttack);
        }
        
        EndAbility();
    }

}
