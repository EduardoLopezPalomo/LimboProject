using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CheckDistanceVector", story: "distance between [Self] and [TargetLocation] [Condition] [value]", category: "Conditions", id: "6b00554f96dfee8c5964b3b6acc66463")]
public partial class CheckDistanceVectorCondition : Condition
{
    [SerializeReference] public BlackboardVariable<Transform> Self;
    [SerializeReference] public BlackboardVariable<Vector3> TargetLocation;
    [Comparison(comparisonType: ComparisonType.All)]
    [SerializeReference] public BlackboardVariable<ConditionOperator> Condition;
    [SerializeReference] public BlackboardVariable<float> Value;

    public override bool IsTrue()
    {
        if (Self.Value == null || TargetLocation.Value == null)
        {
            return false;
        }

        float distance = Vector3.Distance(Self.Value.position, TargetLocation.Value);
        return ConditionUtils.Evaluate(distance, Condition, Value.Value);
    }

}
