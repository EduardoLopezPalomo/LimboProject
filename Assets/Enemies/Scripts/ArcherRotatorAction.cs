using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ArcherRotator", story: "[RotateToPlayer] is use to rotate", category: "Action", id: "0969fe5851c9d03492865d97c5d11ab8")]
public partial class ArcherRotatorAction : Action
{
    [SerializeReference] public BlackboardVariable<RotateToPlayer> RotateToPlayer;

    protected override Status OnUpdate()
    {
        
        if (RotateToPlayer.Value.RotateUntilShoot())
        {
            return Status.Running;
        }

        return Status.Success;
    }
}

