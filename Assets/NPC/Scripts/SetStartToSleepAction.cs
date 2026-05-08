using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SetStartToSleep", story: "[NPCGraphBridge] set NPC is sleeping [bool]", category: "Action", id: "25b8ee48e8bdb248336e0162f5826de4")]
public partial class SetStartToSleepAction : Action
{
    [SerializeReference] public BlackboardVariable<NPCGraphBridge> NPCGraphBridge;
    [SerializeReference] public BlackboardVariable<bool> Bool;
    protected override Status OnStart()
    {
        NPCGraphBridge.Value.SetReachedHome(Bool.Value);
        return Status.Success;
    }
}

