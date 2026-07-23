using UnityEngine;

public class GeneralHealthResponse : AttributeResponse
{
    public override void OnAttributeChanged(GameplayAttributeInstance attributeInstance, float oldValue, AttributeSystem owner, bool CanResponse)
    {
        if(attributeInstance.CurrentValue > attributeInstance.Data.BaseValue)
        {
            OverHealthBehavior(owner);
        }
        if (attributeInstance.CurrentValue < oldValue)
        {
            HurtBehavior(owner, CanResponse);
        }
        if (attributeInstance.CurrentValue <= 0)
        {
            DeathBehavior(owner);
        }
    }

    protected virtual void HurtBehavior(AttributeSystem owner, bool CanResponse)
    {
        
    }

    protected virtual void DeathBehavior(AttributeSystem owner)
    {
       
    }

    protected virtual void OverHealthBehavior(AttributeSystem owner)
    {
        
    }
}
