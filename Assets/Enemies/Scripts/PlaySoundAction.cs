using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PlaySound", story: "play [sound] in [self] transform", category: "Action", id: "2e288c295d7e5e217b1ddb8f7a373545")]
public partial class PlaySoundAction : Action
{
    [SerializeReference] public BlackboardVariable<SFXDefinition> Sound;
    [SerializeReference] public BlackboardVariable<GameObject> self;

    protected override Status OnStart()
    {
        SoundFXManager.instance.PlaySoundRandomPitch(Sound.Value.sfx, self.Value.transform, Sound.Value.volume, Sound.Value.minPitch, Sound.Value.maxPitch);
        return Status.Success;
    }
}

