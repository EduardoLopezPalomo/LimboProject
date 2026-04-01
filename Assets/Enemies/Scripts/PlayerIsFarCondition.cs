using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "PlayerIsFar", story: "Player in [EnemyDetection] is far", category: "Conditions", id: "91842ad9e2b481c5f4170ff44ee017b9")]
public partial class PlayerIsFarCondition : Condition
{
    [SerializeReference] public BlackboardVariable<EnemyPlayerDetection> EnemyDetection;

    public override bool IsTrue()
    {
        return !EnemyDetection.Value.IsPlayerDetected();
    }
}
