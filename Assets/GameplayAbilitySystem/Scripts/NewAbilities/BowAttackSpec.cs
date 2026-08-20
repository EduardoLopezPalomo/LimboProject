using System;
using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class BowAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private RotateToPlayer rotateToPlayer;
    private BowController weaponController;
    private AttributeSystem attributeSystem;
    private GameplayEffectManager effectManager;
    private CharacterSFX characterSFX;
    private GameObject arrow;
    private GameObject SpecialArrow;
    private ArrowTrayectory arrowPath;
    private GameplayEffect SpecialEffect;
    
    private float arrowSpeed;
    private float specialArrowSpeed;
    private float probSpecialAttack;

    private int ComboIndex = 1;
    private bool SpecialAttack = false;
    private float ResistanceNeeded;

    public BowAttackSpec(GameplayAbility ability, GameObject owner,  GameObject arrow, GameObject specialArrow, float probSpecial, GameplayEffect effect)
        : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        characterSFX = owner.GetComponent<CharacterSFX>();
        weaponController = owner.GetComponent<BowController>();
        rotateToPlayer = owner.GetComponent<RotateToPlayer>();
        attributeSystem = owner.GetComponent<AttributeSystem>();
        effectManager = owner.GetComponent<GameplayEffectManager>();
        this.arrow = arrow;
        SpecialArrow = specialArrow;
        arrowPath = owner.GetComponentInChildren<ArrowTrayectory>();
        
        SpecialEffect = effect;
        probSpecialAttack = probSpecial;
        ResistanceNeeded = Math.Abs(SpecialEffect.Value);
        
        ArrowMovement arrowMovementPrefab = arrow.GetComponent<ArrowMovement>();
        if (arrowMovementPrefab != null)
            arrowSpeed = arrowMovementPrefab.InitialSpeed;
        
        ArrowMovement specialArrowMovementPrefab = SpecialArrow.GetComponent<ArrowMovement>();
        if (specialArrowMovementPrefab != null)
            specialArrowSpeed = specialArrowMovementPrefab.InitialSpeed;
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();
        ComboIndex = 1;
        rotateToPlayer.IsShooting = false;
        weaponController.EnableConstaint();
        
        if (CanDoSpecialAttack())
        {
            effectManager.ApplyEffect(SpecialEffect, attributeSystem);
            SpecialAttack = true;
            animator.SetTrigger("SpecialAttack"); 
        }
        else
        {
            SpecialAttack = false;
            animator.SetBool("InCombo", true);
            animator.SetInteger("ComboIndex", ComboIndex);
            animator.SetTrigger("Attack");    
        }
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
        animator.SetBool("InCombo", false);
        AlreadyShoot();
    }

    private bool CanDoSpecialAttack()
    {
        float resistance = attributeSystem.GetAttributeValue("BlockResistance");

        if (resistance > ResistanceNeeded)
        {
            return Random.value < probSpecialAttack;
        }
        
        return false;
    }

    // Animation Events

    public void AlreadyShoot()
    {
        weaponController.DisableConstaint();
    }

    public void ShowArrowPath()
    {
        if (arrowPath == null) return;
        
        arrowPath.Show(owner.transform, SpecialAttack ? specialArrowSpeed : arrowSpeed, SpecialAttack);
    }
    
    public void ShootArrow()
    {
        arrowPath?.Hide();
        characterSFX.SpawnSFX(SFXType.Attack);
        
        GameObject newArrow = Object.Instantiate(SpecialAttack ? SpecialArrow : arrow, weaponController.RightHandSlot.transform.position + new Vector3(0f, 0.5f, 0f), Quaternion.LookRotation(owner.transform.forward));
        ArrowMovement arrowMovement = newArrow.GetComponent<ArrowMovement>();
        if (arrowMovement != null)
        {
            arrowMovement.Init(owner);
            arrowMovement.Shoot(owner.transform.forward);   
        }
        
        if (ComboIndex < 3 && !SpecialAttack)
        {
            animator.SetInteger("ComboIndex", ++ComboIndex);
            return;
        }
        
        rotateToPlayer.IsShooting = true;
        EndAbility();
    }

}
