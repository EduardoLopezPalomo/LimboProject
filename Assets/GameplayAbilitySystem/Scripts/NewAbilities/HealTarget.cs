using UnityEngine;
using UnityEngine.Animations;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/HealTarget")]
public class HealTarget : GameplayAbility
{
    public int maxEnemiesDetection = 5;
    public float radiusDetection = 3f;
    public float minHealthToStart = 90f;
    public GameplayEffect healEffect;
    public GameObject healAreaPrefab;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new HealTargetSpec(this, owner, maxEnemiesDetection, radiusDetection, healEffect, healAreaPrefab, minHealthToStart);
    }
}
