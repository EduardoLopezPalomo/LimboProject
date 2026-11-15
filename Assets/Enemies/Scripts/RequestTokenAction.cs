using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RequestToken", story: "Request Token from [controller] and write [token]", category: "Action", id: "3e0cc0dde4a03281f8e5aa7e2733026c")]
public partial class RequestTokenAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemiesGroupController> Controller;
    [SerializeReference] public BlackboardVariable<bool> Token;

    protected override Status OnStart()
    {
        // Null by design - see the note in AlertGroupAction: a scene object cannot be authored into a
        // shared graph or prefab, so resolving from the hierarchy is the intended path and stays silent.
        EnemiesGroupController controller = Controller != null ? Controller.Value : null;
        if (controller == null)
        {
            controller = EnemiesGroupController.Resolve(base.GameObject);
            if (controller == null)
            {
                LogFailure($"No group controller found above {EnemiesGroupController.Describe(base.GameObject)} - it cannot request an attack token.", true);
                return Status.Failure;
            }
            if (Controller != null) Controller.Value = controller;
        }

        if (controller.RequestToken())
        {
            if (Token != null) Token.Value = true;
            return Status.Success;
        }
        if (Token != null) Token.Value = false;
        return Status.Failure;
    }

}

