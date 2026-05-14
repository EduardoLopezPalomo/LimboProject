using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ForceSleepNPC", story: "[NPCPool] force sleep", category: "Action", id: "eb6ef25ef2952ea98532edaf7049f632")]
public partial class ForceSleepNpcAction : Action
{
    [SerializeReference] public BlackboardVariable<NPCPool> NPCPool;

    protected override Status OnStart()
    {
        NPCPool.Value.ForceSleep();
        return Status.Success;
    }
}

