using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/DamageProyectile")]
public class DamageProyectile : GameplayAbility
{

    public float angle = 25f;
    public float distanceOffset = 1f;
    public VFXDefinition proyectile;

    public float backDashDistance = 3f;
    public float dashTime = 0.5f;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new DamageProyectileSpec(this, owner, proyectile, angle, distanceOffset, backDashDistance, dashTime, Mathf.Abs(EffectsToSelf[0].Value));
    }
}
