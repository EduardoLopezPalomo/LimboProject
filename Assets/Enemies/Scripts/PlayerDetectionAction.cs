using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Player detection", story: "Is player detected by [PlayerDetection]", category: "Action", id: "e44fa14f52c632c28fd721adbfff6d1f")]
public partial class PlayerDetectionAction : Action
{
    [SerializeReference] public BlackboardVariable<PlayerDetection> PlayerDetection;

    protected override Status OnStart()
    {
        return PlayerDetection.Value.IsPlayerDetected() ? Status.Success : Status.Failure;
    }

}

