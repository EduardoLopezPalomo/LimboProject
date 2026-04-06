using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "StartTokenTimer", story: "[TokenTimer] Start", category: "Action", id: "b2a3643ad2ebca144acbf2f90f2311ea")]
public partial class StartTokenTimerAction : Action
{
    [SerializeReference] public BlackboardVariable<TokenTimer> TokenTimer;

    protected override Status OnStart()
    {
        TokenTimer.Value.StartCooldown();
        return Status.Success;
    }

}

