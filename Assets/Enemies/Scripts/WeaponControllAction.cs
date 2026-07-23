using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "WeaponControll", story: "Use [WeaponControl] to equp [true]", category: "Action", id: "fc97f6bd04f62d1933a7c06805030092")]
public partial class WeaponControllAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyWeaponController> WeaponControl;
    [SerializeReference] public BlackboardVariable<bool> True;

    protected override Status OnStart()
    {
        if (True.Value)
        {
            WeaponControl.Value.EquipWeapon();
        }
        else
        {
            WeaponControl.Value.UnequipWeapon();
        }
        return Status.Success;
    }
    
}

