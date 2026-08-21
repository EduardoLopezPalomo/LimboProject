using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PutShieldUp", story: "Use [shieldController] and [EnemyShieldController] to put shield up", category: "Action", id: "9f0097eb5f28fd0d999d5b26d35594c3")]
public partial class PutShieldUpAction : Action
{
    [SerializeReference] public BlackboardVariable<ShieldMaskController> ShieldController;
    [SerializeReference] public BlackboardVariable<EnemyShieldController> enemyShieldController;

    protected override Status OnStart()
    {
        ShieldController.Value.EnableShieldMask();
        enemyShieldController.Value.EquipWeapon();
        return Status.Success;
    }
    
}

