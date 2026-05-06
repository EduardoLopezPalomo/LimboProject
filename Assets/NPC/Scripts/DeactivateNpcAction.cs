using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "DeactivateNPC", story: "Use [NPCPool] to deactivate", category: "Action", id: "14f84566f300dfbcac314a6055c94e80")]
public partial class DeactivateNpcAction : Action
{
    [SerializeReference] public BlackboardVariable<NPCPool> NPCPool;

    protected override Status OnStart()
    {
       if (NPCPool.Value != null)
        {
            PoolManager.Instance.Sleep(NPCPool.Value);
        }
        return Status.Success;
    }
}

