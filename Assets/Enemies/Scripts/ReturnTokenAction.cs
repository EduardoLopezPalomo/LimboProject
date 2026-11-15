using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ReturnToken", story: "Return Token to [controller] if [Token] is true", category: "Action", id: "92d51d660bb7c45b467784acbeda4d48")]
public partial class ReturnTokenAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemiesGroupController> Controller;
    [SerializeReference] public BlackboardVariable<bool> token;

    protected override Status OnStart()
    {
        // token / Controller may be null bindings, not just null Values - see the note in AlertGroupAction.
        if (token == null)
        {
            LogFailure($"'{nameof(token)}' is not bound on this ReturnToken node - an attack token may stay spent.", true);
            return Status.Failure;
        }

        if (token.Value)
        {
            EnemiesGroupController controller = Controller != null ? Controller.Value : null;
            if (controller == null)
            {
                controller = EnemiesGroupController.Resolve(base.GameObject);
                if (controller == null)
                {
                    LogFailure($"No group controller found above {EnemiesGroupController.Describe(base.GameObject)} - the attack token is lost.", true);
                    return Status.Failure;
                }
                if (Controller != null) Controller.Value = controller;
            }

            controller.ReturnToken();
            token.Value = false;
        }
        return Status.Success;
    }

 
}

