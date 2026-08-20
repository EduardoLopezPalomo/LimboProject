using UnityEngine;
using StarterAssets;
using System.Collections;
using UnityEngine.AI;

public class ArcherDashSpec : GameplayAbilitySpec
{
    private int dashDistance;
    private Animator animator;
    private MakeInvincible makeInvincible;
    private RotateAlongPlayer rotateAlongPlayer;
    private LayerMask obstacleMask;

    private Vector3 direction;
    private bool dashEnded;

    public ArcherDashSpec(GameplayAbility ability, GameObject owner, int dashForce, LayerMask obstacle) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        makeInvincible = owner.GetComponent<MakeInvincible>();
        rotateAlongPlayer =  owner.GetComponent<RotateAlongPlayer>();
        
        dashDistance = dashForce;
        obstacleMask = obstacle;
    }

    public override bool CanActivateAbility()
    {
        return !bOnCooldown;
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();
        
        animator.applyRootMotion = true;
        direction = ChooseDodgeDirection(owner.transform.position, rotateAlongPlayer.player.position, dashDistance);
        RotateTowardsDashDirection();
        animator.SetTrigger("Dash");
        makeInvincible.SetInvincible(true);

        dashEnded = false;
    }
    
    
    Vector3 ChooseDodgeDirection(Vector3 archerPos, Vector3 playerPos, float dodgeDist)
    {
        Vector3 toArcher = (archerPos - playerPos).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, toArcher);

        Vector3[] candidates = {
            archerPos + right * dodgeDist,              
            archerPos - right * dodgeDist,               
            archerPos + (right + toArcher).normalized * dodgeDist,  
            archerPos - (right - toArcher).normalized * dodgeDist,  
        };
        
        for (int i = candidates.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        foreach (var c in candidates)
        {
            if (NavMesh.SamplePosition(c, out var hit, 1.5f, NavMesh.AllAreas))
            {
                if (!Physics.CheckSphere(hit.position, 0.4f, obstacleMask))
                {
                    float newDist = Vector3.Distance(hit.position, playerPos);
                    if (newDist > Vector3.Distance(archerPos, playerPos))
                        return hit.position; 
                }
            }
        }
        return archerPos; // sin escape válido 
    }
    
    private void RotateTowardsDashDirection()
    {
        if(direction.sqrMagnitude < 0.001f)
            return;

        owner.transform.rotation = Quaternion.LookRotation(direction);
    }
    

    public override void CancelAbility()
    {
        base.CancelAbility();
        
        EndDash();
    }

    public void EndDash()
    {
        if (dashEnded) return;
        dashEnded = true;
        animator.applyRootMotion = false;
        makeInvincible.SetInvincible(false);
        
        EndAbility();
    }
}
