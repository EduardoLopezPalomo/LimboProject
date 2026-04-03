using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Succes", story: "Succes", category: "Action", id: "da6e5244375bb5e4326e25ce75db6141")]
public partial class SuccesAction : Action
{

    protected override Status OnStart()
    {
        return Status.Success;
    }
    
}

