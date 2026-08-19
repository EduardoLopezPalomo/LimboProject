using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RequestToken", story: "Request Token from [controller] and write [token]", category: "Action", id: "3e0cc0dde4a03281f8e5aa7e2733026c")]
public partial class RequestTokenAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemiesGroupContoller> Controller;
    [SerializeReference] public BlackboardVariable<bool> Token;

    protected override Status OnStart()
    {
        if (Controller.Value.RequestToken())
        {
            Token.Value = true;
            return Status.Success;
        }
        Token.Value = false;
        return Status.Failure;
    }

}

