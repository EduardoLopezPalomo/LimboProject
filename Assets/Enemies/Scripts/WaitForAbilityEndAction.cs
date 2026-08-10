using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Wait for Ability End", story: "In [AbilityHolder] has finnish [Ability] for [MaxTime]", category: "Action", id: "22917f32ac38db121ad87b02314f4e4e")]
public partial class WaitForAbilityEndAction : Action
{
    [SerializeReference] public BlackboardVariable<AbilityHolder> AbilityHolder;
    [SerializeReference] public BlackboardVariable<string> Ability;
    [SerializeReference] public BlackboardVariable<float> MaxTime;
    
    private float actualTime = 0f;

    protected override Status OnStart()
    {
        actualTime = 0f;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!AbilityHolder.Value.IsAbilityActive(Ability.Value))
        {
            return Status.Success;   
        }
        
        actualTime += Time.deltaTime;
        if (actualTime > MaxTime)
        {
            return Status.Failure;
        }
        
        return Status.Running;
    }

}

