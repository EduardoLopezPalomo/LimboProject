using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ChangeMinAngleRotation", story: "Change [value] to MinAngleRotation of [RotateToPlayer]", category: "Action", id: "cafe921c2e1a0e557267b6b9828bd855")]
public partial class ChangeMinAngleRotationAction : Action
{
    [SerializeReference] public BlackboardVariable<float> Value;
    [SerializeReference] public BlackboardVariable<RotateToPlayer> RotateToPlayer;

    protected override Status OnStart()
    {
        RotateToPlayer.Value.MinAngle = Value.Value;
        return Status.Success;
    }
}

