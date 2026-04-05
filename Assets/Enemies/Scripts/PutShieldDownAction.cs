using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PutShieldDown", story: "Use [ShieldController] to put shield down", category: "Action", id: "f8155cee79150a614c73a00800c41652")]
public partial class PutShieldDownAction : Action
{
    [SerializeReference] public BlackboardVariable<ShieldMaskController> ShieldController;

    protected override Status OnStart()
    {
        ShieldController.Value.DisableShieldMask();
        return Status.Success;
    }

    
}

