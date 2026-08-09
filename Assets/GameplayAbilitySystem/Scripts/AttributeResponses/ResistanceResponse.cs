using UnityEngine;
using System;

[CreateAssetMenu(menuName = "AbilitySystem/Responses/ResistanceResponse")]
public class ResistanceResponse : AttributeResponse
{
    public static event Action<GameObject> OnResistanceBack;
    public static event Action<GameObject, float> OnResistanceConsumed;
    public override void OnAttributeChanged(GameplayAttributeInstance attributeInstance, float oldValue, AttributeSystem owner, bool CanResponse, float duration = 0f)
    {
        if (attributeInstance.CurrentValue > oldValue)
        {
            OnResistanceBack?.Invoke(owner.gameObject);
        }
        else if (attributeInstance.CurrentValue < oldValue)
        {
            OnResistanceConsumed?.Invoke(owner.gameObject, duration);
        }
        
        EnemyUIbridge UI = owner.GetComponent<EnemyUIbridge>();
        if (UI != null)
        {
            UI.ShowResistance();
        }
        
    }
}
