using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ExecuteAttackSequence", story: "Use [AttackDirector] to use ability in [AbilityHolder] for max [MaxTime]", category: "Action", id: "d1544a2e07d96b9fb38ba7e148163982")]
public partial class ExecuteAttackSequenceAction : Action
{
    [SerializeReference] public BlackboardVariable<AttackDirector> AttackDirector;
    [SerializeReference] public BlackboardVariable<AbilityHolder> AbilityHolder;
    [SerializeReference] public BlackboardVariable<float> MaxTime;
    
    private AttackSequences currentSequence;
    private int stepIndex;
    private float actualTime;
    
    protected override Status OnStart()
    {
        currentSequence = AttackDirector.Value.PickSequence();
        if (currentSequence == null || currentSequence.steps.Length == 0)
            return Status.Failure;

        stepIndex = 0;
        actualTime = 0f;
        ActivateCurrentStep();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        string key = currentSequence.steps[stepIndex].ability;

        if (!AbilityHolder.Value.IsAbilityActive(key))
        {
            stepIndex++;
            if (stepIndex >= currentSequence.steps.Length)
            {
                return Status.Success; 
            }
            actualTime = 0f;
            ActivateCurrentStep();
            return Status.Running;
        }

        actualTime += Time.deltaTime;
        if (actualTime > MaxTime.Value)
        {
            return Status.Failure;
        }

        return Status.Running;
    }


    private void ActivateCurrentStep()
    {
        string key = currentSequence.steps[stepIndex].ability;
        AbilityHolder.Value.ActivateAbility(key, AbilityHolder.Value.gameObject);
    }
}

