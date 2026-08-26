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

    private Dictionary<string, GameplayAttributeInstance> _lookup;
    private List<GameplayAttributeInstance> _recoveredAttributes;

    private void Awake()
    {
        _lookup = new Dictionary<string, GameplayAttributeInstance>();
        _recoveredAttributes = new List<GameplayAttributeInstance>();

        foreach (var attribute in attributes)
        {
            attribute.Initialize(this);
            if (!_lookup.ContainsKey(attribute.Data.Name))
            {
                _lookup.Add(attribute.Data.Name, attribute);
                if (attribute.Data.CanRecover)
                {
                    _recoveredAttributes.Add(attribute);
                }
            }
            else
            {
                Debug.LogWarning($"AttributeSystem: duplicate attribute data {attribute.Data.name}");
            }
        }
    }
    /*Add the modifier to the attribute instance, that way it can change*/
    public void AddModifier(string name, Guid id, Modifier modifier, float value, bool AttributeCanResponse, float duration)
    {
        if (!_lookup.TryGetValue(name, out var inst))
        {
            Debug.LogWarning("AttributeSystem: attribute not found: " + name);
            return;
        }

        inst.AddModifier(id, modifier, value, AttributeCanResponse, duration);
    }
    /*Remove the modifier in order to go back to the previous value*/
    public void RemoveModifier(string name, Guid id, bool AttributeCanResponse)
    {
        if (!_lookup.TryGetValue(name, out var inst))
        {
            Debug.LogWarning("AttributeSystem: attribute not found: " + name);
            return;
        }

        inst.RemoveModifier(id, AttributeCanResponse);
    }
    /*Apply the effect only if its infinite*/
    public void ApplyEffectOnce(GameplayEffect effect, bool notify = true)
    {
        if (effect == null) return;
        if (!_lookup.TryGetValue(effect.TargetAttribute, out var inst))
        {
            Debug.LogWarning("AttributeSystem: attribute not found: " + effect.TargetAttribute);
            return;
        }
        if (effect.EffectTime == EffectTime.Infinite)
        {
            inst.ModifyRuntimeBase(effect.Value, effect.Modifier, effect.CanAttributeResponse, notify);
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
    
    public GameplayAttributeData GetAttributeData(String name)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.Data.Name == name)
            {
                return attribute.Data;
            }
        }
        Debug.LogWarning("AttributeSystem: attribute not found: " + name);
        return null;
    }
    
    public Dictionary<string, float> CaptureValues()
    {
        var snapshot = new Dictionary<string, float>();
        foreach (var attribute in attributes)
        {
            snapshot[attribute.Data.Name] = attribute.CurrentValue;
        }
        return snapshot;
    }

    public void ApplyValues(Dictionary<string, float> values, bool notify)
    {
        foreach (var attribute in attributes)
        {
            if (values.TryGetValue(attribute.Data.Name, out float value))
            {
                attribute.ModifyRuntimeBase(value, Modifier.Override, false, notify);
            }
        }
    }

    private void Update()
    {
        foreach (var attribute in _recoveredAttributes)
        {
            attribute.Tick(Time.deltaTime);
        }
    }
}
