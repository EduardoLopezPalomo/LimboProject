using System;
using UnityEngine;

public class ExplosionDamage : MonoBehaviour
{
    public float DamageTickRate = 2f;
    public float lifeTime = 3f;
    [SerializeField] private GameplayEffect DamageEffect;
    [SerializeField] private string ObjectToDamage;

    private bool isIn = false;
    private float tickTimer = 0f;
    private MakeInvincible invincible;
    private GameplayEffectManager effectmanager;
    private AttributeSystem attributeSystem;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(ObjectToDamage))
        {
            invincible = other.gameObject.GetComponent<MakeInvincible>();
            effectmanager = other.gameObject.GetComponent<GameplayEffectManager>();
            attributeSystem = other.gameObject.GetComponent<AttributeSystem>();
            if (effectmanager == null || attributeSystem == null)
            {
                Debug.Log("effectmanager, attributesystem or makeInvincible is null");
                return;
            }
            isIn = true;
            tickTimer = 1.8f;
        }
    }

    void OnTriggerExit(Collider other)
    {
        isIn = false;
    }

    void Update()
    {
        if (isIn)
        {
            tickTimer += Time.deltaTime;
            if (tickTimer >= DamageTickRate)
            {
                tickTimer = 0f;
                if (invincible != null && invincible.IsInvincible())
                {
                    return;
                }
           
                effectmanager.ApplyEffect(DamageEffect, attributeSystem);
            }
        }
    }

    
}
