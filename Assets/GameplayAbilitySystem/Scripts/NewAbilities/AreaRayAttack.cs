using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/AreaRayAttack")]
public class AreaRayAttack : GameplayAbility
{
    public float radius = 5f;

    public int maxEnemiesHit = 5;

    public GameplayEffect DamageEffect;
    public GameObject spherePrefab;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new AreaRayAttackSpec(this, owner, radius, DamageEffect, maxEnemiesHit, spherePrefab);
    }
}
