using UnityEngine;
using System;

[CreateAssetMenu(menuName = "AbilitySystem/Responses/ResistanceResponse")]
public class ResistanceResponse : AttributeResponse
{
    public override void OnAttributeChanged(GameplayAttributeInstance attributeInstance, float oldValue, AttributeSystem owner, bool CanResponse, float duration = 0f)
    {
        if (owner.gameObject.CompareTag("Player"))
        {
            if (attributeInstance.CurrentValue <= 0)
            {
                KnockBack knockBack = owner.GetComponent<KnockBack>();
                if (knockBack != null)
                {
                    knockBack.StartKnockBack();
                }
            }
        }
        else
        {
             EnemyUIbridge UI = owner.GetComponent<EnemyUIbridge>();
             if (UI != null)
             {
                 UI.ShowResistance();
             }   
             if (attributeInstance.CurrentValue <= 0)
             {
                 var enemy = owner.GetComponent<EnemyGraphBridge>();
                 if (enemy)
                 {
                     enemy.SetVulnerable();
                 }
             }
        }
        
    }
}
