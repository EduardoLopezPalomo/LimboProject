using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AlertGroup", story: "Use [GroupController] to alert by [Self]", category: "Action", id: "3f767a766d07210ec38f056e0e791bb7")]
public partial class AlertGroupAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemiesGroupController> GroupController;
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    protected override Status OnStart()
    {
        // This value is null by design, not by omission: every enemy prefab serializes the graph variable
        // with m_Value: {fileID: 0} (see Barbarian_fem1.prefab), and no one can author anything else there,
        // because the group controller is a scene object and a prefab cannot reference one. So resolving it
        // from the hierarchy is the intended path, not a workaround, and it is silent.
        // It is also why the group's own value can never be cached here: the node's [SerializeReference]
        // field was dropped when the T-026 sweep renamed EnemiesGroupContoller -> EnemiesGroupController.
        EnemiesGroupController controller = GroupController != null ? GroupController.Value : null;
        UnityEngine.GameObject subject = Self != null && Self.Value != null ? Self.Value : base.GameObject;
        if (controller == null)
        {
            controller = EnemiesGroupController.Resolve(subject);
            if (controller == null)
            {
                // The only genuinely broken case: an enemy that is not inside any group at all.
                LogFailure($"No group controller found above {EnemiesGroupController.Describe(subject)} - the group cannot be alerted.", true);
                return Status.Failure;
            }
            if (GroupController != null) GroupController.Value = controller;
        }

        controller.AlertGroup(subject);
        return Status.Success;
    }

}

