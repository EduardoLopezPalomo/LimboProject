using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Escape from player", story: "[EscapeFromPlayer] set value of [Target]", category: "Action", id: "1c64de04fcaa8cddc5d641107d568a6a")]
public partial class ScapeFromPlayerAction : Action
{
    [SerializeReference] public BlackboardVariable<EscapeFromPlayer> EscapeFromPlayer;
    [SerializeReference] public BlackboardVariable<Vector3> Target;

    protected override Status OnStart()
    {
        Target.Value = EscapeFromPlayer.Value.GetScapePosition();
        return Status.Success;
    }
    
}

