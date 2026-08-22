using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CheckBossPhase", story: "[PhaseController] writes in [phase]", category: "Action", id: "2993df70bb68b6a47f0910d5d0095ee4")]
public partial class CheckBossPhaseAction : Action
{
    [SerializeReference] public BlackboardVariable<PhaseController> PhaseController;
    [SerializeReference] public BlackboardVariable<int> Phase;

    protected override Status OnStart()
    {
        BossPhase phase = PhaseController.Value.CurrentPhase;

        switch (phase)
        {
            case BossPhase.Phase1 :
                Phase.Value = 1;
                break;
            case BossPhase.Phase2 :
                Phase.Value = 2;
                break;
            case BossPhase.Phase3 :
                Phase.Value = 3;
                break;
        }
        return Status.Success;
    }

}

