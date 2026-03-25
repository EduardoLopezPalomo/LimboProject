using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/DamageProyectile")]
public class DamageProyectile : GameplayAbility
{

    public float angle = 25f;

    public float lateralOffset = 1f;
    public GameObject proyectile;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new DamageProyectileSpec(this, owner, proyectile, angle, lateralOffset);
    }
}
