using System;
using System.Collections.Generic;
using UnityEngine;
/*
 *  It controls the modification of the attibutes
 *  It has a list of attibutes and a dictionary in order to aply effects to the target
 * 
 */
public class AttributeSystem : MonoBehaviour
{

    [SerializeField] private List<GameplayAttributeInstance> attributes;

    private Dictionary<GameplayAttributeData, GameplayAttributeInstance> _lookup;

    private void Awake()
    {
        _lookup = new Dictionary<GameplayAttributeData, GameplayAttributeInstance>();

        foreach (var attribute in attributes)
        {
            attribute.Initialize(this);
            if (!_lookup.ContainsKey(attribute.Data))
            {
                _lookup.Add(attribute.Data, attribute);
            }
            else
            {
                Debug.LogWarning($"AttributeSystem: duplicate attribute data {attribute.Data.name}");
            }
        }
    }
    /*Add the modifier to the attribute instance, that way it can change*/
    public void AddModifier(GameplayAttributeData data, Guid id, Modifier modifier, float value, bool AttributeCanResponse, float duration)
    {
        if (!_lookup.TryGetValue(data, out var inst))
        {
            Debug.LogWarning("AttributeSystem: attribute not found: " + data?.name);
            return;
        }

        inst.AddModifier(id, modifier, value, AttributeCanResponse, duration);
    }
    /*Remove the modifier in order to go back to the previous value*/
    public void RemoveModifier(GameplayAttributeData data, Guid id, bool AttributeCanResponse)
    {
        if (!_lookup.TryGetValue(data, out var inst))
        {
            Debug.LogWarning("AttributeSystem: attribute not found: " + data?.name);
            return;
        }

        inst.RemoveModifier(id, AttributeCanResponse);
    }
    /*Apply the effect only if its infinite*/
    public void ApplyEffectOnce(GameplayEffect effect)
    {
        if (effect == null) return;
        if (!_lookup.TryGetValue(effect.TargetAttribute, out var inst))
        {
            Debug.LogWarning("AttributeSystem: attribute not found: " + effect.TargetAttribute?.name);
            return;
        }
        if (effect.EffectTime == EffectTime.Infinite)
        {
            inst.ModifyRuntimeBase(effect.Value, effect.Modifier, effect.CanAttributeResponse);
        }
        else
        {
            Debug.LogWarning("ApplyEffectOnce should not be called for Duration effects");
        }
    }
    /*Get the value of the attribute*/
    public float GetAttributeValue(String name)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.Data.Name == name)
            {
                return attribute.CurrentValue;
            }
        }
        Debug.LogWarning("AttributeSystem: attribute not found: " + name);
        return 0f;
    }
    /*get the max value of an attribute*/
    public float GetAttributeMaxValue(String name)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.Data.Name == name)
            {
                return attribute.Data.MaxValue;
            }
        }
        Debug.LogWarning("AttributeSystem: attribute not found: " + name);
        return 0f;
    }
}
