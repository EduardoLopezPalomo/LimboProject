using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "HasHitPlayer", story: "[WeaponController] says if has hit player", category: "Conditions", id: "3cb8f0592abae92c3ee840acb45c9301")]
public partial class HasHitPlayerCondition : Condition
{
    [SerializeReference] public BlackboardVariable<EnemyWeaponController> WeaponController;

    public override bool IsTrue()
    {
        if (WeaponController.Value.WeaponDamage)
        {
            return WeaponController.Value.WeaponDamage.GetHasHitPlayer();   
        }
        
        return false;
    }

}
