using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CanRestNPC", story: "[NPCPool] says that can rest", category: "Conditions", id: "f1d4ae7e3e73d2b7902a8065c31bede9")]
public partial class CanRestNpcCondition : Condition
{
    [SerializeReference] public BlackboardVariable<NPCPool> NPCPool;

    public override bool IsTrue()
    {
        return NPCPool.Value.RestAtHome;
    }
}
