using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ActivateHUD", story: "Use [UIBridge] to show HUD", category: "Action", id: "5774bad4f3eef13978ea58cf80a6d9de")]
public partial class ActivateHudAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyUIbridge> UIBridge;

    protected override Status OnStart()
    {
        UIBridge.Value.ShowHealth();
        UIBridge.Value.ShowResistance();
        return Status.Success;
    }
}

