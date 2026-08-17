using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SpawnVFX", story: "Use [CharacterVFX] to spawn [Type] and write on [IsFireAura] and [VFX]", category: "Action", id: "caf67d60557d1d9701b2d264d8150222")]
public partial class SpawnVfxAction : Action
{
    [SerializeReference] public BlackboardVariable<CharacterVFX> CharacterVFX;
    [SerializeReference] public BlackboardVariable<VFXType> Type;
    [SerializeReference] public BlackboardVariable<bool> IsFireAura;
    [SerializeReference] public BlackboardVariable<GameObject> VFX;

    protected override Status OnStart()
    {
        var (chosen, vfxInstance) = CharacterVFX.Value.SpawnRandomVFX(Type.Value);
        
        VFX.Value = vfxInstance;
        
        switch (chosen?.variantId)
        {
            case "Fire":
                IsFireAura.Value = true;
                break;
            case "Electro":
                IsFireAura.Value = false;
                break;
        }
        return Status.Success;
    }

}

