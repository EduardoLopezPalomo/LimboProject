using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Failure", story: "Failure", category: "Action", id: "ac2bc47491a31a2e365a75fafe84dfe2")]
public partial class FailureAction : Action
{

    protected override Status OnStart()
    {
        return Status.Failure;
    }

}

