using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "GetAttribute", story: "use [AttributeSystem] to get [Atribute] in [value]", category: "Action", id: "e7d77cbd70dec62dc796ad2a64f75285")]
public partial class GetAttributeAction : Action
{
    [SerializeReference] public BlackboardVariable<AttributeSystem> AttributeSystem;
    [SerializeReference] public BlackboardVariable<string> Atribute;
    [SerializeReference] public BlackboardVariable<float> Value;

    protected override Status OnStart()
    {
        Value.Value = AttributeSystem.Value.GetAttributeValue(Atribute.Value);
        return Status.Success;
    }

}

