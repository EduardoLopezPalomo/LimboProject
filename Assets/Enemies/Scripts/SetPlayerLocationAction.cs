using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Set player location", story: "[GoingToPlayer] set value of [PlayerLocation]", category: "Action", id: "1c45a17f0752a711c48ae5358217ef83")]
public partial class SetPlayerLocationAction : Action
{
    [SerializeReference] public BlackboardVariable<GoingToPlayer> GoingToPlayer;
    [SerializeReference] public BlackboardVariable<Vector3> PlayerLocation;

    protected override Status OnStart()
    {
        PlayerLocation.Value = GoingToPlayer.Value.GetPlayerPosition();
        return Status.Success;
    }
}

