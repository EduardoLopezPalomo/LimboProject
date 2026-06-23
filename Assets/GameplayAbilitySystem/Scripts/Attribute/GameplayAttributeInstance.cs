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

    public void Initialize(AttributeSystem attributeSystem)
    {
        owner = attributeSystem;
        RuntimeBaseValue = Data != null ? Data.BaseValue : 0f;
        RecalculateCurrent();
    }
    /* Add the type of modifier in his diccionary in order to remove it later*/
    public void AddModifier(Guid id, Modifier modifier, float value, bool AttributeCanResponse)
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
        RecalculateCurrent(AttributeCanResponse);
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
    private void RecalculateCurrent(bool AttributeCanResponse = false)
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
                if(Data.Response != null)
                {
                    Data.Response.OnAttributeChanged(this, OldValue, owner, AttributeCanResponse);
                }
                Debug.Log("Current value of" + Data.Name + " " + CurrentValue);
                return;
            }
        }

        float sumAdds = 0f;
        foreach (var v in addModifiers.Values) sumAdds += v;

        float productMult = 1f;
        foreach (var v in multModifiers.Values) productMult *= v;

        CurrentValue = Mathf.Clamp((RuntimeBaseValue + sumAdds) * productMult, 0f, Data.MaxValue); 
        if (Data.Response != null)
        {
            Data.Response.OnAttributeChanged(this, OldValue, owner, AttributeCanResponse);
        }
        Debug.Log("Current value of"+ Data.Name + " " + CurrentValue);
    }
    /*It modifies the value of the instance not of the data if its infinite*/
    public void ModifyRuntimeBase(float deltaOrValue, Modifier mode, bool AttributeCanResponse)
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
        RecalculateCurrent(AttributeCanResponse);
    }
}
