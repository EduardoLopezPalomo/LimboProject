using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/MageCounterAttack")]
public class MageCounterAttack : GameplayAbility
{
    public VFXDefinition IceCounter;
    
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new MageCounterAttackSpec(this, owner, IceCounter);
    }
}
