using System.Collections;
using Unity.Behavior;
using UnityEngine;

public class BarbarianAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private EnemyWeaponController weaponController;
    private BehaviorGraphAgent behaviorGraphAgent;
    private AttributeSystem attributeSystem;
    private GameplayEffectManager effectManager;
    private CharacterSFX characterSFX;
    private float timeStuned;
    private float specialAttackChance;
    private int comboIndex;
    private GameplayEffect effect;

    private Coroutine StunnedCoroutine;
    
    private string healthAttributeName = "EnemyHealth";
    private string resistanceAttributeName = "BlockResistance";
    
    private float maxHealth;
    private float maxResistance;

    public BarbarianAttackSpec(GameplayAbility ability, GameObject owner, float TimeStuned, float SpecialAttackChance, GameplayEffect SpecialAttackEffect) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        weaponController = owner.GetComponent<EnemyWeaponController>();
        behaviorGraphAgent = owner.GetComponent<BehaviorGraphAgent>();
        characterSFX = owner.GetComponent<CharacterSFX>();
        attributeSystem = owner.GetComponent<AttributeSystem>();
        effectManager = owner.GetComponent<GameplayEffectManager>();
        timeStuned = TimeStuned;
        specialAttackChance = SpecialAttackChance;
        effect = SpecialAttackEffect;
        
        maxHealth = attributeSystem.GetAttributeMaxValue(healthAttributeName);
        maxResistance = attributeSystem.GetAttributeMaxValue(resistanceAttributeName);
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();

        StartCombo();
    }

    void StartCombo()
    {

        if (!animator.GetBool("Grounded"))
        {
            return;
        }
        weaponController.WeaponDamage.SetHasHitPlayer(false);
        animator.SetBool("InCombo", true);
        if (ShouldDoSpecialAttack())
        {
            comboIndex = 2;
            effectManager.ApplyEffect(effect, attributeSystem);
            animator.SetInteger("AttackNumber", 1);
        }
        else
        {
            comboIndex = 0;
            animator.SetInteger("AttackNumber", 0);
        }
        animator.SetTrigger("Attack");
        weaponController.EquipWeapon();
    }

    void EndCombo()
    {
        if (!animator.GetBool("IsStrafing"))
        {
            weaponController.UnequipWeapon();
        }
        animator.SetBool("InCombo", false);
    }

    public override void CancelAbility()
    {
        base.CancelAbility();

        animator.SetBool("InCombo", false);
        DisableSwordCollider();
    }

    public override void EndAbility()
    {
        base.EndAbility();
        EndCombo();
    }
    
    bool ShouldDoSpecialAttack()
    {
        float chance = specialAttackChance;
        float resistanceRatio = attributeSystem.GetAttributeValue(resistanceAttributeName) / maxResistance;
        float healthRatio = attributeSystem.GetAttributeValue(healthAttributeName) / maxHealth;
        
        chance += (1f - healthRatio) * 0.2f; // hasta +30%
        
        chance += (resistanceRatio - 0.5f) * 0.4f;// penaliza hasta -20%
    
        return Random.value < Mathf.Clamp01(chance);
    }

    IEnumerator StayStunned()
    {
        animator.speed = 0f;
        yield return new WaitForSeconds(timeStuned);

        animator.speed = 1f;
    }

    public void StartStunned()
    {
        if (weaponController.WeaponDamage.GetHasHitPlayer())
        {
            return;
        }

        if (StunnedCoroutine != null)
        {
            weaponController.StopCoroutine(StunnedCoroutine);
        }
        behaviorGraphAgent.BlackboardReference.SetVariableValue("AttackingState", AttackingState.Vulnerable);
        StunnedCoroutine = weaponController.StartCoroutine(StayStunned());
    }


    public void EnableSwordCollider()
    {
        weaponController.SetComboNumber(comboIndex);
        comboIndex++;
        if (comboIndex >= 2)
        {
            owner.gameObject.transform.Rotate(0f, 10f, 0f);
        }
        weaponController.EnableCollision();
        characterSFX.SpawnRandomSFX(SFXType.Attack);
    }

    public void DisableSwordCollider()
    {

        weaponController.DisableCollision();
    }
}
