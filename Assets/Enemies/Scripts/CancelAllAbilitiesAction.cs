using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CancelAllAbilities", story: "[AbilityHolder] cancels all abilities", category: "Action", id: "3b66e065e64b93f1246c15c4afe64652")]
public partial class CancelAllAbilitiesAction : Action
{
    [SerializeReference] public BlackboardVariable<AbilityHolder> AbilityHolder;

    protected override Status OnStart()
    {
        AbilityHolder.Value.CancelAllAbility();
        return Status.Success;
    }

}

