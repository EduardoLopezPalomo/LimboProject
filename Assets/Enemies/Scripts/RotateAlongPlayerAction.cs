using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RotateAlongPlayer", story: "[Rotate] for this [myTime]", category: "Action", id: "a93c938be5067d19cb33075097a87d57")]
public partial class RotateAlongPlayerAction : Action
{
    [SerializeReference] public BlackboardVariable<RotateAlongPlayer> Rotate;
    [SerializeReference] public BlackboardVariable<float> myTime;

    private float ActualTime = 0f;

    protected override Status OnStart()
    {
        ActualTime = 0f;
        Rotate.Value.StartRotating();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        ActualTime += Time.deltaTime;
        if (ActualTime <= myTime.Value)
        {
            return Status.Running;
        }
        else
        {
            Rotate.Value.StopRotating();
            return Status.Success;
        }
    }

    protected override void OnEnd()
    {
        Rotate.Value.StopRotating();
    }
}

