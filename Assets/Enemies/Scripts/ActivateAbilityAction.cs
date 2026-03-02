using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Activate Ability", story: "Use [AbilityHolder] to activate this [Ability]", category: "Action", id: "e42daad5e62ffdcab4c2a922434fcdf9")]
public partial class ActivateAbilityAction : Action
{
    [SerializeReference] public BlackboardVariable<AbilityHolder> AbilityHolder;
    [SerializeReference] public BlackboardVariable<string> Ability;

    protected override Status OnStart()
    {
        AbilityHolder.Value.ActivateAbility(Ability.Value, AbilityHolder.Value.gameObject);
        return Status.Success;
    }
}

