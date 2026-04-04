using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RotateToTarget", story: "[Self] [Rotate] to target", category: "Action", id: "dbd14d21dbc3b55706e60a7349ce54a9")]
public partial class RotateToTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<RotateToTarget> Rotate;

    protected override Status OnUpdate()
    {
        Rotate.Value.RotateTowardsTarget();

        if (Rotate.Value.hasPlayerRotate)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    
}

