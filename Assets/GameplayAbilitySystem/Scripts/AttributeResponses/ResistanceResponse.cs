using UnityEngine;
using System;

[CreateAssetMenu(menuName = "AbilitySystem/Responses/ResistanceResponse")]
public class ResistanceResponse : AttributeResponse
{
    public static event Action<GameObject> OnResistanceBack;
    public override void OnAttributeChanged(GameplayAttributeInstance attributeInstance, float oldValue, AttributeSystem owner, bool CanResponse)
    {
        if (attributeInstance.CurrentValue > oldValue)
        {
            OnResistanceBack?.Invoke(owner.gameObject);
        }
        
    }
}
