using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/LightAura")]
public class LightAura : GameplayAbility
{
    public float radius = 5f;

    public int maxEnemiesHit = 5;

    public GameplayEffect DamageEffect;
    public GameObject spherePrefab;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new LightAuraSpec(this, owner, radius, DamageEffect, maxEnemiesHit, spherePrefab);
    }
}
