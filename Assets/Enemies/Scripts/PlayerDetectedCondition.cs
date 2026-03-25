using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "PlayerDetected", story: "Player is [close] to self", category: "Conditions", id: "56597fd65a3b49afef5e69acf034892f")]
public partial class PlayerDetectedCondition : Condition
{
    [SerializeReference] public BlackboardVariable<EnemyPlayerDetection> Close;

    public override bool IsTrue()
    {
        return Close.Value.IsPlayerDetected();
    }

}
