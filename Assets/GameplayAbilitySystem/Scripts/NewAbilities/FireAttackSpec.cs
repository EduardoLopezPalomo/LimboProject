using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

public class FireAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private GameObject firePrefab;
    private float inclineAngle;

    public FireAttackSpec(GameplayAbility ability, GameObject owner, GameObject firePrefab, float inclineAngle) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        this.firePrefab = firePrefab;
        this.inclineAngle = inclineAngle;
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();
        
        animator.SetTrigger("Attack"); 
    }

    public override void CancelAbility()
    {
        base.CancelAbility();
    }

    // Animation Events
    
    public void SpawnFire()
    {
        Vector3 spawnPosition = owner.transform.position + owner.transform.forward * 0.8f + Vector3.up * 1.0f;
        GameObject fireInstance = Object.Instantiate(firePrefab, spawnPosition, Quaternion.identity);
        fireInstance.transform.forward = (owner.transform.forward + Vector3.up * inclineAngle).normalized; 
        
        MoveFireBall Fire = fireInstance.GetComponent<MoveFireBall>();
        if (Fire != null)
        {
            Fire.Init(owner);
        }
        
        EndAbility();
    }

}
