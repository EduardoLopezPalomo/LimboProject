using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "RotateHasTarget", story: "[Rotate] has Target selected", category: "Conditions", id: "3063cc9b20ce4a7add908172839b0219")]
public partial class RotateHasTargetCondition : Condition
{
    [SerializeReference] public BlackboardVariable<RotateToTarget> Rotate;

    public override bool IsTrue()
    {
        return Rotate.Value.HasTarget();
    }

    
}
