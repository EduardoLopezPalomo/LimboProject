using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Responses/EnemyHealth")]
public class EnemyHealthResponse : GeneralHealthResponse
{
    protected override void HurtBehavior(AttributeSystem owner, bool CanResponse)
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

    protected override void DeathBehavior(AttributeSystem owner)
    {
        DeathCheck deathCheck = owner.gameObject.GetComponent<DeathCheck>();
        if (deathCheck.GetIsDeath()) return;
        
        deathCheck.SetIsDeath(true);
        GameObject target = owner.gameObject;
        EnemyGraphBridge enemy = owner.GetComponent<EnemyGraphBridge>();
                
        enemy.SetDeath();
                
        KarmaManager.Instance.AddKarma(-10);
                
        Destroy(target, 2f); 
    }
    
    protected override void OverHealthBehavior(AttributeSystem owner)
    {
        EnemyUIbridge UI = owner.GetComponent<EnemyUIbridge>();
        if (UI != null)
        {
            UI.ShowHealth();
        }
    }
}
