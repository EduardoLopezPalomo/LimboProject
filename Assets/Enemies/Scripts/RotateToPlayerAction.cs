using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Rotate to player", story: "[Agent] [rotate] to player", category: "Action", id: "afe8e0253722bb9163c0dcd03bcced26")]
public partial class RotateToPlayerAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<RotateToPlayer> Rotate;

    protected override Status OnUpdate()
    {
        Rotate.Value.RotateTowardsPlayer();

        if (Rotate.Value.hasPlayerRotate)
        {
            return Status.Success;
        }

        return Status.Running;
    }

}

