using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CancelVFX", story: "[CharacterVFX] cancels this [VFX]", category: "Action", id: "549ea4a55364ddf0cc30a29999713f85")]
public partial class CancelVfxAction : Action
{
    [SerializeReference] public BlackboardVariable<CharacterVFX> CharacterVFX;
    [SerializeReference] public BlackboardVariable<GameObject> VFX;

    protected override Status OnStart()
    {
        if (VFX.Value == null) return Status.Success;
        
        CharacterVFX.Value.CancelVFX(VFX.Value);
        return Status.Success;
    }

}

