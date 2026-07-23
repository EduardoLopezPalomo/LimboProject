using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

public class HealTargetSpec : GameplayAbilitySpec
{
    private Animator animator;
    private RootMotionController rootMotion;
    private ParticleController particleController;
    private GameplayEffect healEffect;
    
    private GameObject healAreaPrefab;
    private float radiusDetection;
    private float minHealthToStart;
    private LayerMask enemyLayer = LayerMask.GetMask("EnemyLayer");
    private Collider[] results;

    private GameplayEffectManager finalEffectManager;
    private AttributeSystem finalAttributeSystem;
    public HealTargetSpec(GameplayAbility ability, GameObject owner, int maxEnemiesDetection, float radius, GameplayEffect healEffect, GameObject healAreaPrefab, float minHealtStart) : base(ability, owner)
    {
        animator = owner.GetComponent<Animator>();
        rootMotion = owner.GetComponent<RootMotionController>();
        particleController = owner.GetComponent<ParticleController>();
        results = new Collider[maxEnemiesDetection];
        radiusDetection = radius;
        minHealthToStart = minHealtStart;
        this.healEffect = healEffect;
        this.healAreaPrefab = healAreaPrefab;
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();
        int hits = Physics.OverlapSphereNonAlloc(owner.transform.position, radiusDetection, results, enemyLayer);

        int enemySelected = -1;
        float minHealth = float.MaxValue;
        
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
                if(attributeSystem.GetAttributeValue("Health") < minHealth && attributeSystem.GetAttributeValue("Health") <= minHealthToStart)
                {
                    minHealth = attributeSystem.GetAttributeValue("Health");
                    enemySelected = i;
                }
            }
        }
        AttributeSystem myAttributeSystem = owner.GetComponent<AttributeSystem>();
        if (myAttributeSystem.GetAttributeValue("Health") < minHealth && myAttributeSystem.GetAttributeValue("Health") <= minHealthToStart)
        {
            finalEffectManager = owner.GetComponent<GameplayEffectManager>();
            finalAttributeSystem = myAttributeSystem;
            
        }
        else if(enemySelected != -1)
        {
            finalEffectManager = results[enemySelected].gameObject.GetComponent<GameplayEffectManager>();
            finalAttributeSystem= results[enemySelected].gameObject.GetComponent<AttributeSystem>();
            
        }
        else
        {
            return;
        }
        particleController.ActivateParticle();
        SetTargetToRotate();
        ShowHealAreaVisual(finalEffectManager.transform.position, 4f);
        animator.SetBool("IsHealing", true);
        animator.SetTrigger("Attack"); 
    }
    
    private void ShowHealAreaVisual(Vector3 position, float duration)
    {
        GameObject healArea = Object.Instantiate(healAreaPrefab, position, Quaternion.identity);
        healArea.transform.SetParent(finalEffectManager.transform);
        Object.Destroy(healArea, duration);
    }

    private void SetTargetToRotate()
    {
        GameplayEffectManager myEffectManager = owner.GetComponent<GameplayEffectManager>();
        RotateToTarget rotateToTarget = owner.GetComponent<RotateToTarget>();
        if (rotateToTarget == null && finalEffectManager == null && myEffectManager == null)
        {
            return;
        }

        if (finalEffectManager != myEffectManager)
        {
            rotateToTarget.SetTarget(finalEffectManager.gameObject);
        }
        else
        {
            rotateToTarget.SetTarget(null);
        }
    }


    // Animation Events
    
    public void HealTarget()
    {
        if (finalEffectManager != null && finalAttributeSystem != null)
        {
            finalEffectManager.ApplyEffect(healEffect, finalAttributeSystem);
        }
    }
    public void StopHealParticle()
    {
        particleController.DeactivateParticle();
        EndAbility();
    }
    
}
