using UnityEngine;


[CreateAssetMenu(menuName = "AbilitySystem/Responses/Health")]
public class HealthResponse : AttributeResponse
{
    public override void OnAttributeChanged(GameplayAttributeInstance attributeInstance, float oldValue, AttributeSystem owner, bool CanResponse)
    {
        if(attributeInstance.Data.name == "Health" && attributeInstance.CurrentValue > attributeInstance.Data.BaseValue)
        {
            OverHealthBehavior(owner);
        }
        if (attributeInstance.Data.name == "Health" && attributeInstance.CurrentValue < oldValue)
        {
            HurtBehavior(owner, CanResponse);
        }
        if (attributeInstance.Data.name == "Health" && attributeInstance.CurrentValue <= 0)
        {
            DeathBehavior(owner);
        }
    }

    private void HurtBehavior(AttributeSystem owner, bool CanResponse)
    {
        if (owner.CompareTag("Enemy"))
        {
            if (CanResponse)
            {
                var enemy = owner.GetComponent<EnemyGraphBridge>();
                if (enemy != null)
                {
                    enemy.SetHurt();
                }   
            }

            EnemyUIbridge UI = owner.GetComponent<EnemyUIbridge>();
            if (UI != null)
            {
                UI.ShowHealth();
            }
        }

        if (!CanResponse) return;
        
        KnockBack knockBack = owner.GetComponent<KnockBack>();
        if (knockBack)
        {
            knockBack.StartKnockBack();   
        }
    }

    private void DeathBehavior(AttributeSystem owner)
    {
        DeathCheck deathCheck = owner.gameObject.GetComponent<DeathCheck>();
        if (deathCheck.GetIsDeath()) return;
            
        if (owner.CompareTag("Enemy"))
        {
            deathCheck.SetIsDeath(true);
            GameObject target = owner.gameObject;
            EnemyGraphBridge enemy = owner.GetComponent<EnemyGraphBridge>();
                
            enemy.SetDeath();
                
            KarmaManager.Instance.AddKarma(-10);
                
            Destroy(target, 2f); 
        }
        else if (owner.CompareTag("Player"))
        {
            KnockBack knockBack = owner.GetComponent<KnockBack>();
                
            knockBack.StartKnockBack();
        }
    }

    private void OverHealthBehavior(AttributeSystem owner)
    {
        EnemyUIbridge UI = owner.GetComponent<EnemyUIbridge>();
        if (UI != null)
        {
            UI.ShowHealth();
        }
    }
}
