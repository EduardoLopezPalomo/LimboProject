using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

public class BowAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private RootMotionController rootMotion;
    private RotateToPlayer rotateToPlayer;
    private BowController weaponController;
    private GameObject arrow;
    private ArrowTrayectory arrowPath;
    private float arrowSpeed;

    public BowAttackSpec(GameplayAbility ability, GameObject owner,  GameObject arrow)
        : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        rootMotion = owner.GetComponent<RootMotionController>();
        weaponController = owner.GetComponent<BowController>();
        rotateToPlayer = owner.GetComponent<RotateToPlayer>();
        this.arrow = arrow;
        arrowPath = owner.GetComponentInChildren<ArrowTrayectory>();
        
        ArrowMovement arrowMovementPrefab = arrow.GetComponent<ArrowMovement>();
        if (arrowMovementPrefab != null)
            arrowSpeed = arrowMovementPrefab.InitialSpeed;
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();
        rotateToPlayer.IsShooting = false;
        weaponController.EnableConstaint();
        animator.SetBool("InCombo", true);
        animator.SetTrigger("Attack"); 
    }
    
    public override void CancelAbility()
    {
        base.CancelAbility();
        arrowPath?.Hide();
        animator.SetBool("InCombo", false);
        AlreadyShoot();

    }

    public override void EndAbility()
    {
       base.EndAbility();
    }

    // Animation Events

    public void AlreadyShoot()
    {
        weaponController.DisableConstaint();
        rotateToPlayer.IsShooting = true;
    }

    public void ShowArrowPath()
    {
        if (arrowPath == null) return;
        
        arrowPath.Show(owner.transform, arrowSpeed);
    }
    
    public void ShootArrow()
    {
        arrowPath?.Hide();
        
        GameObject newArrow = Object.Instantiate(arrow, weaponController.RightHandSlot.transform.position + new Vector3(0f, 0.5f, 0f), Quaternion.LookRotation(owner.transform.forward));
        ArrowMovement arrowMovement = newArrow.GetComponent<ArrowMovement>();
        if (arrowMovement != null)
        {
            arrowMovement.Init(owner);
            arrowMovement.Shoot(owner.transform.forward);   
        }
    }

}
