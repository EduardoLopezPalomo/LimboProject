using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Apply Effect ", story: "[EffectManager] use to apply [Effect] in [Attribute]", category: "Action", id: "269e815553d88436c7b64d80094ecad9")]
public partial class ApplyEffectAction : Action
{
    [SerializeReference] public BlackboardVariable<GameplayEffectManager> EffectManager;
    [SerializeReference] public BlackboardVariable<GameplayEffect> Effect;
    [SerializeReference] public BlackboardVariable<AttributeSystem> Attribute;

    protected override Status OnStart()
    {
        if (EffectManager == null || Effect == null || Attribute == null)
        {
            Debug.LogError("EffectManager, Effect, or Attribute is not assigned.");
            return Status.Failure;
        }
        
        EffectManager.Value.ApplyEffect(Effect.Value, Attribute.Value);
        return Status.Success;
    }
}

