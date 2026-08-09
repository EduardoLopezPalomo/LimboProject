using UnityEngine;

public abstract class AttributeResponse : ScriptableObject
{
    public abstract void OnAttributeChanged(GameplayAttributeInstance attributeInstance, float oldValue, AttributeSystem owner, bool CanResponse, float duration = 0f);
}
