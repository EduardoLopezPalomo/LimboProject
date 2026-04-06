using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "GoingToSlots", story: "[GoingPlayer] use [slotType] to set [PlayerLocation]", category: "Action", id: "0efdf32212f49b0621ff65fa4f01ad93")]
public partial class GoingToSlotsAction : Action
{
    [SerializeReference] public BlackboardVariable<GoingToPlayer> GoingPlayer;
    [SerializeReference] public BlackboardVariable<Vector3> PlayerLocation;
    [SerializeReference] public BlackboardVariable<SlotType> SlotType;

    protected override Status OnStart()
    {
        GoingPlayer.Value.slotType = this.SlotType;
        PlayerLocation.Value = GoingPlayer.Value.GetSlotPosition();
        return Status.Success;
    }

   
}

