using System;
using System.Collections.Generic;
using UnityEngine;

/*  It has the instance of the attribute data 
 *  This instance is the one that is modified depending on the EffectTime and Modifier
 */
[System.Serializable]
public class GameplayAttributeInstance
{
    public GameplayAttributeData Data;

    public float RuntimeBaseValue { get; private set; }
    public float CurrentValue { get; private set; }

    private AttributeSystem owner;

    private Dictionary<Guid, float> addModifiers = new();
    private Dictionary<Guid, float> multModifiers = new();
    private Dictionary<Guid, float> overrideModifiers = new();
    private Dictionary<Guid, long> overrideTimestamps = new();
    
    private bool justApplyInfModifier = false;
    private float auxTimer = 0f;

    public void Initialize(AttributeSystem attributeSystem)
    {
        owner = attributeSystem;
        RuntimeBaseValue = Data != null ? Data.BaseValue : 0f;
        RecalculateCurrent();
    }
    /* Add the type of modifier in his diccionary in order to remove it later*/
    public void AddModifier(Guid id, Modifier modifier, float value, bool AttributeCanResponse, float duration)
    {
        switch (modifier)
        {
            case Modifier.Add:
                addModifiers[id] = value;
                break;
            case Modifier.Multiply:
                multModifiers[id] = value;
                break;
            case Modifier.Override:
                overrideModifiers[id] = value;
                overrideTimestamps[id] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                break;
        }
        RecalculateCurrent(AttributeCanResponse, duration);
    }
    /* Remove the modifier and the attribute goes back to the previous value*/
    public void RemoveModifier(Guid id, bool AttributeCanResponse)
    {
        var removed = false;
        removed |= addModifiers.Remove(id);
        removed |= multModifiers.Remove(id);
        removed |= overrideModifiers.Remove(id);
        overrideTimestamps.Remove(id);

        if (removed) RecalculateCurrent(AttributeCanResponse);
    }

    /*Recalculate the current value of the attribute if some effects is active*/
    private void RecalculateCurrent(bool AttributeCanResponse = false, float duration = 0f, bool notify = true)
    {
        float OldValue = CurrentValue;

        if (overrideModifiers.Count > 0)
        {
            Guid last = Guid.Empty;
            long maxTs = long.MinValue;
            foreach (var kv in overrideTimestamps)
            {
                if (kv.Value > maxTs)
                {
                    maxTs = kv.Value;
                    last = kv.Key;
                }
            }
            if (last != Guid.Empty && overrideModifiers.TryGetValue(last, out var ov))
            {
                CurrentValue = Mathf.Clamp(ov, 0f, Data.MaxValue);
                if(notify && Data.Response != null)
                {
                    Data.Response.OnAttributeChanged(this, OldValue, owner, AttributeCanResponse, duration);
                }
                //Debug.Log("Current value of" + Data.Name + " " + CurrentValue);
                return;
            }
        }

        float sumAdds = 0f;
        foreach (var v in addModifiers.Values) sumAdds += v;

        float productMult = 1f;
        foreach (var v in multModifiers.Values) productMult *= v;

        CurrentValue = Mathf.Clamp((RuntimeBaseValue + sumAdds) * productMult, 0f, Data.MaxValue); 
        if (notify && Data.Response != null)
        {
            Data.Response.OnAttributeChanged(this, OldValue, owner, AttributeCanResponse, duration);
        }
        // Debug.Log("Current value of"+ Data.Name + " " + CurrentValue);
    }
    /*It modifies the value of the instance not of the data if its infinite*/
    public void ModifyRuntimeBase(float deltaOrValue, Modifier mode, bool AttributeCanResponse, bool notify = true )
    {
        switch (mode)
        {
            case Modifier.Add:
                RuntimeBaseValue += deltaOrValue;
                break;
            case Modifier.Multiply:
                RuntimeBaseValue *= deltaOrValue;
                break;
            case Modifier.Override:
                RuntimeBaseValue = deltaOrValue;
                break;
        }
        RuntimeBaseValue = Mathf.Clamp(RuntimeBaseValue, 0f, Data.MaxValue);
        justApplyInfModifier = true;
        auxTimer = Data.WaitTillRecover;
        RecalculateCurrent(AttributeCanResponse, 0, notify);
    }
    
    public void Tick(float deltaTime)
    {
        if (CurrentValue >= Data.MaxValue)
            return;

        if (justApplyInfModifier)
        {
            auxTimer -= deltaTime;

            if (auxTimer <= 0f)
            {
                justApplyInfModifier = false;
            }
            else
            {
                return;
            }
        }
        
        RuntimeBaseValue += Data.RecoverValuePerSecond * deltaTime;
        RuntimeBaseValue = Mathf.Clamp(RuntimeBaseValue, 0f, Data.MaxValue);

        RecalculateCurrent(false, 0f);
    }
}
