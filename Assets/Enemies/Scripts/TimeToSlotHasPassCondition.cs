using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "TimeToSlotHasPass", story: "[Time] has pass", category: "Conditions", id: "9bb6f1853ce5e0c3a201a93b78a1e316")]
public partial class TimeToSlotHasPassCondition : Condition
{
    [SerializeReference] public BlackboardVariable<SlotUpdate> Time;

    public override bool IsTrue()
    {
        return Time.Value.CanUpdateSlot();
    }

}
