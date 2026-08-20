using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/ArcherDash")]
public class ArcherDash : GameplayAbility
{
    public int dashDistance = 10;
    public LayerMask obstacleMask;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new ArcherDashSpec(this, owner, dashDistance, obstacleMask);
    }
}
