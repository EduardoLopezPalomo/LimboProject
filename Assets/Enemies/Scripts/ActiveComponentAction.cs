using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ActiveComponent", story: "Set [component] [active]", category: "Action", id: "35834810d3ddd6ccf7074c7cc6fe89be")]
public partial class ActiveComponentAction : Action
{
    [SerializeReference] public BlackboardVariable<MinimapIcon> Component;
    [SerializeReference] public BlackboardVariable<bool> Active;

    protected override Status OnStart()
    {
        if (Component.Value == null)
        {
            LogFailure("No object assigned.");
            return Status.Failure;
        }

        Component.Value.enabled = Active.Value;
        return Status.Success;
    }
}

