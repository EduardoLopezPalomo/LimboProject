using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ComboController", story: "Control counter of [ComboIndex]", category: "Action", id: "013346a7a862f612486cfa8c681c8c37")]
public partial class ComboControllerAction : Action
{
    [SerializeReference] public BlackboardVariable<int> ComboIndex;

    protected override Status OnStart()
    {
        if (ComboIndex.Value < 3)
        {
            ComboIndex.Value++;
        }
        else
        {
            ComboIndex.Value = 1;
        }
        return Status.Success;
    }
}

