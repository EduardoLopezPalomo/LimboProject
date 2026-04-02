using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

public class BowAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private RootMotionController rootMotion;
    private BowController weaponController;
    private GameObject arrow;

    public BowAttackSpec(GameplayAbility ability, GameObject owner,  GameObject arrow )
        : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        rootMotion = owner.GetComponent<RootMotionController>();
        weaponController = owner.GetComponent<BowController>();
        this.arrow = arrow;
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();
       
        weaponController.EnableConstaint();
        animator.SetBool("InCombo", true);
        animator.SetTrigger("Attack"); 
    }

    public override void EndAbility()
    {
       base.EndAbility();
    }

    // Animation Events

    public void EnableRootMotion() => rootMotion.EnableAnimRootMotion();
    public void DisableRootMotion() => rootMotion.DisableAnimRootMotion();

    public void AlreadyShoot()
    {
        weaponController.DisableConstaint();
    }
    
    public void ShootArrow()
    {
        GameObject newArrow = Object.Instantiate(arrow, weaponController.RightHandSlot.transform.position + new Vector3(0f, 0.5f, 0f), Quaternion.LookRotation(owner.transform.forward));
        ArrowMovement arrowMovement = newArrow.GetComponent<ArrowMovement>();
        if (arrowMovement != null)
        {
            arrowMovement.Init(owner);
            arrowMovement.Shoot(owner.transform.forward);   
        }
    }

}
