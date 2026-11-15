using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "NotifyEndBattle", story: "[GroupController] is notify that is calm", category: "Action", id: "2df2a095e6074a5a921e06d95b3fccea")]
public partial class NotifyEndBattleAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemiesGroupController> GroupController;

    protected override Status OnStart()
    {
        // Null by design - see the note in AlertGroupAction.
        EnemiesGroupController controller = GroupController != null ? GroupController.Value : null;
        if (controller == null)
        {
            controller = EnemiesGroupController.Resolve(base.GameObject);
            if (controller == null)
            {
                LogFailure($"No group controller found above {EnemiesGroupController.Describe(base.GameObject)} - the group cannot be told the chase is over.", true);
                return Status.Failure;
            }
            if (GroupController != null) GroupController.Value = controller;
        }

        controller.EnemyEndPersecution();
        return Status.Success;
    }

}

