using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "NotifyEndBattle", story: "[GroupController] is notify that is calm", category: "Action", id: "2df2a095e6074a5a921e06d95b3fccea")]
public partial class NotifyEndBattleAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemiesGroupContoller> GroupController;

    protected override Status OnStart()
    {
        GroupController.Value.EnemyEndPersecution();
        return Status.Success;
    }

}

