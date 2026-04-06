using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ReturnToken", story: "Return Token to [controller]", category: "Action", id: "92d51d660bb7c45b467784acbeda4d48")]
public partial class ReturnTokenAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemiesGroupContoller> Controller;

    protected override Status OnStart()
    {
        Controller.Value.ReturnToken();
        return Status.Success;
    }

 
}

