using UnityEngine;

public class AreaRayAttackSpec : GameplayAbilitySpec
{
    private float radius;
    private GameObject spherePrefab;
    private GameplayEffect damageEffect;

    private LayerMask enemyLayer = LayerMask.GetMask("EnemyLayer");

    private Collider[] results;
    public AreaRayAttackSpec(GameplayAbility ability, GameObject owner, float radius, GameplayEffect DamageEffect, int maxEnemiesHit, GameObject spherePrefab) : base(ability, owner)
    {
        this.radius = radius;
        this.damageEffect = DamageEffect;
        this.spherePrefab = spherePrefab;
        results = new Collider[maxEnemiesHit];
    }
    public override void ActivateAbility()
    {
        base.ActivateAbility();
        int hits = Physics.OverlapSphereNonAlloc(owner.transform.position, radius, results, enemyLayer);
        ShowSphereVisual(owner.transform.position, radius, 2f);

        for (int i = 0; i < hits; i++)
        {
            GameplayEffectManager effectmanager = results[i].gameObject.GetComponent<GameplayEffectManager>();
            AttributeSystem attributeSystem = results[i].gameObject.GetComponent<AttributeSystem>();

            if (effectmanager == null || attributeSystem == null)
            {
                continue;
            }

            if (results[i].gameObject.CompareTag("Enemy"))
            {
                effectmanager.ApplyEffect(damageEffect, attributeSystem);
            }
        }
    }

    private void ShowSphereVisual(Vector3 position, float radius, float duration)
    {
        GameObject sphere = Object.Instantiate(spherePrefab, position, Quaternion.identity);
        sphere.transform.localScale = Vector3.one * radius * 2f;

        Object.Destroy(sphere, duration);
    }

    public override void EndAbility()
    {
        base.EndAbility();
    }
}
