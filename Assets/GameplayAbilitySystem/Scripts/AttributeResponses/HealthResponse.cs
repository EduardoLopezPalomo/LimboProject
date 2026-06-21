using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "AbilitySystem/Responses/Health")]
public class HealthResponse : AttributeResponse
{
    public override void OnAttributeChanged(GameplayAttributeInstance attributeInstance, float oldValue, AttributeSystem owner, bool CanResponse)
    {
        if (attributeInstance.Data.name == "Health" && attributeInstance.CurrentValue < oldValue)
        {
            if (!CanResponse) return;
            if (owner.CompareTag("Enemy"))
            {
                var enemy = owner.GetComponent<EnemyGraphBridge>();
                if (enemy != null)
                {
                    enemy.SetHurt();
                }    
            }
            
            KnockBack knockBack = owner.GetComponent<KnockBack>();
            if (knockBack)
            {
                knockBack.StartKnockBack();   
            }
            
        }
        if (attributeInstance.Data.name == "Health" && attributeInstance.CurrentValue <= 0)
        {
            DeathCheck deathCheck = owner.gameObject.GetComponent<DeathCheck>();
            if (deathCheck.GetIsDeath()) return;
            
            if (owner.CompareTag("Enemy"))
            {
                deathCheck.SetIsDeath(true);
                GameObject target = owner.gameObject;
                Animator animator = target.GetComponent<Animator>();
                NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
                EnemyGraphBridge enemy = owner.GetComponent<EnemyGraphBridge>();
                enemy.SetTrap();
                agent.isStopped = true;
                agent.velocity = Vector3.zero;

                animator.SetTrigger("IsDeath");

                KarmaManager.Instance.AddKarma(-10);

                Destroy(target, 2f); 
            }
            else if (owner.CompareTag("Player"))
            {
                KnockBack knockBack = owner.GetComponent<KnockBack>();
                
                knockBack.StartKnockBack();
            }
        }
    }
}
