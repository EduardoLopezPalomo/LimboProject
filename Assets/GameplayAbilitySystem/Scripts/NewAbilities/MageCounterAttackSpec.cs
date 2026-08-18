using System;
using System.Collections;
using Unity.Behavior;
using UnityEngine;

public class MageCounterAttackSpec : GameplayAbilitySpec
{
    private Animator animator;
    private AttributeSystem attributeSystem;
    private RotateAlongPlayer rotateAlongPlayer;
    
    private VFXDefinition IceCounter;


    public MageCounterAttackSpec(GameplayAbility ability, GameObject owner, VFXDefinition counter) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        attributeSystem = owner.GetComponent<AttributeSystem>();
        rotateAlongPlayer = owner.GetComponent<RotateAlongPlayer>();
        
        IceCounter = counter;
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();
        if (attributeSystem.GetAttributeValue("BlockResistance") > 0f)
        {
            StartCounter();
        }
        else
        {
            EndAbility();
        }
    }

    void StartCounter()
    {

        if (!animator.GetBool("Grounded"))
        {
            return;
        }
        animator.SetTrigger("CounterAttack");
        
    }

    public void SpawnCounter()
    {
        Vector3 direction = rotateAlongPlayer.player.position - owner.transform.position;
        direction.y = 0;

        Quaternion rotation = Quaternion.LookRotation(direction);
        
        VFXManager.Instance.Play(IceCounter, owner.transform.position, rotation, null);
        EndAbility();
    }

    
}
