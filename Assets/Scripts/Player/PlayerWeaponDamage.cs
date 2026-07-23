using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Events;

public class PlayerWeaponDamage : MonoBehaviour
{
    public UnityEvent EnemyCounter;
    
    private int ComboNumber = 0;
    [Header("Combo effects")]
    [SerializeField] private List<GameplayEffect> gameplayEffects;
    

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Shield"))
        {
            Debug.Log("Hit shield!");
            AbilityHolder abilityHolder = GetComponentInParent<AbilityHolder>();
            Animator animator = GetComponentInParent<Animator>();
            if (abilityHolder != null && animator != null)
            {
                animator.SetBool("InCombo", false);
                animator.SetTrigger("HitShield");
                abilityHolder.CancelAbility("LightAttack");   
            }
            return; 
        }
        if (other.gameObject.layer == LayerMask.NameToLayer("EnemyLayer"))
        {
            DamageEnemy(other);
        }
    }

    private void DamageEnemy(Collider other)
    {
        BehaviorGraphAgent agent = other.gameObject.GetComponent<BehaviorGraphAgent>();
        agent.BlackboardReference.GetVariableValue("AttackingState", out AttackingState state);

        if (state == AttackingState.Defend)
        {
            EnemyCounter.Invoke();
            agent.BlackboardReference.SetVariableValue("AttackingState", AttackingState.CounterAttack);
            return;
        }
        
        GameplayEffectManager effectmanager = other.gameObject.GetComponent<GameplayEffectManager>();
        AttributeSystem attributeSystem = other.gameObject.GetComponent<AttributeSystem>();

        if (ComboNumber < 0 || ComboNumber >= gameplayEffects.Count)
        {
            Debug.LogWarning("the combo is " + ComboNumber + "But the list doent have that number");
            return;
        }
        
        GameplayEffect effect = gameplayEffects[ComboNumber];

        if (effect == null)
        {
            Debug.LogWarning("the combo number has no effect on the player weapon");
            return;
        }
        
        effectmanager.ApplyEffect(effect, attributeSystem);
    }

    public void SetComboNumber(int value)
    {
        ComboNumber = value;
    }

}
