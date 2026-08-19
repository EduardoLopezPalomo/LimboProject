using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ReturnToken", story: "Return Token to [controller] if [Token] is true", category: "Action", id: "92d51d660bb7c45b467784acbeda4d48")]
public partial class ReturnTokenAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemiesGroupContoller> Controller;
    [SerializeReference] public BlackboardVariable<bool> token;

    protected override Status OnStart()
    {
        if (token.Value)
        {
            Controller.Value.ReturnToken();  
            token.Value = false;
        }
        return Status.Success;
    }

 
}

