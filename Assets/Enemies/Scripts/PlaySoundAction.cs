using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PlaySound", story: "Use [CharacterSFX] to play [sound]", category: "Action", id: "2e288c295d7e5e217b1ddb8f7a373545")]
public partial class PlaySoundAction : Action
{
    [SerializeReference] public BlackboardVariable<SFXType> Sound;
    [SerializeReference] public BlackboardVariable<CharacterSFX> characterSFX;

    protected override Status OnStart()
    {
        characterSFX.Value.SpawnRandomSFX(Sound.Value);
        return Status.Success;
    }
}

