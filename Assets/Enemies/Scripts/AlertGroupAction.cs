using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AlertGroup", story: "Use [EnemyGroupContoller] to alert by [Self]", category: "Action", id: "3f767a766d07210ec38f056e0e791bb7")]
public partial class AlertGroupAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemiesGroupContoller> EnemyGroupContoller;
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    protected override Status OnStart()
    {
        EnemyGroupContoller.Value.AlertGroup(Self.Value);
        return Status.Success;
    }

}

