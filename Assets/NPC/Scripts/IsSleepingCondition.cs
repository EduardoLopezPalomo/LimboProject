using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsSleeping", story: "[NPCBridge] says is Sleeping", category: "Conditions", id: "f05fb84a7991de9e834242a8aee4332d")]
public partial class IsSleepingCondition : Condition
{
    [SerializeReference] public BlackboardVariable<NPCGraphBridge> NPCBridge;

    public override bool IsTrue()
    {
        return NPCBridge.Value.GetReachedHome();
    }
}
