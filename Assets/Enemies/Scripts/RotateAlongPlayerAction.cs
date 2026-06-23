using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RotateAlongPlayer", story: "[Rotate] from [MinTime] to [MaxTIme]", category: "Action", id: "a93c938be5067d19cb33075097a87d57")]
public partial class RotateAlongPlayerAction : Action
{
    [SerializeReference] public BlackboardVariable<RotateAlongPlayer> Rotate;
    [SerializeReference] public BlackboardVariable<float> MinTime;
    [SerializeReference] public BlackboardVariable<float> MaxTIme;

    private float Timer = 0.0f;

    protected override Status OnStart()
    {
        Timer = UnityEngine.Random.Range(MinTime, MaxTIme);
        Rotate.Value.StartRotating();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        Timer -= Time.deltaTime;
        if (Timer <= 0)
        {
            Rotate.Value.StopRotating();
            return Status.Success;
        }

        return Status.Running;
        
    }

    protected override void OnEnd()
    {
        Rotate.Value.StopRotating();
    }
}

