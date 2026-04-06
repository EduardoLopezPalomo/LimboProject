using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FreeSlot", story: "[GoingPlayer] free slots", category: "Action", id: "7996efcbe1c3147fcba1064ba01c6fc5")]
public partial class FreeSlotAction : Action
{
    [SerializeReference] public BlackboardVariable<GoingToPlayer> GoingPlayer;

    protected override Status OnStart()
    {
        GoingPlayer.Value.ReleaseSlot();
        return Status.Success;
    }

}

