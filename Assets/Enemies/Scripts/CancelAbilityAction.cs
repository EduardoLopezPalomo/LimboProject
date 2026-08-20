using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CancelAbility", story: "[AbilityHolder] cancels [ability]", category: "Action", id: "b400d1bc0f2d479b2280d97a7aec04f1")]
public partial class CancelAbilityAction : Action
{
    [SerializeReference] public BlackboardVariable<AbilityHolder> AbilityHolder;
    [SerializeReference] public BlackboardVariable<string> Ability;

    protected override Status OnStart()
    {
        AbilityHolder.Value.CancelAbility(Ability.Value);
        return Status.Success;
    }

}

