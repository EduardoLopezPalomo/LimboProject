using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Wait until", story: "Wait until [Variable] [Compare] [Value] for [MaxTime]", category: "Action", id: "881a10eb7f43656a0c3081e072ce7020")]
public partial class WaitUntilAction : Action
{
    [SerializeReference] public BlackboardVariable Variable;
    [Comparison(comparisonType: ComparisonType.BlackboardVariables, variable: "Variable", comparisonValue: "Value")]
    [SerializeReference] public BlackboardVariable<ConditionOperator> Compare;
    [SerializeReference] public BlackboardVariable Value;
    [SerializeReference] public BlackboardVariable<float> MaxTime;
    
    private float actualTime = 0f;

    protected override Status OnStart()
    {
        if (Variable == null)
        {
            return Status.Failure;
        }

        actualTime = 0f;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (ConditionUtils.Evaluate(Variable, Compare, Value))
        {
            return Status.Success;   
        }
        
        actualTime += Time.deltaTime;
        if (actualTime > MaxTime)
        {
            return Status.Failure;
        }
        
        return Status.Running;
    }

}

