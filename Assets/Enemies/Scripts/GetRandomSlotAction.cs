using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Get random Slot", story: "[GoingToPlayer] get random Slot in [position]", category: "Action", id: "f28c0fe94424803b30011b0a10cb6461")]
public partial class GetRandomSlotAction : Action
{
    [SerializeReference] public BlackboardVariable<GoingToPlayer> GoingToPlayer;
    [SerializeReference] public BlackboardVariable<Vector3> Position;

    protected override Status OnStart()
    {
        Position.Value = GoingToPlayer.Value.GetRandomSlot();
        
        return Status.Success;
    }
}

