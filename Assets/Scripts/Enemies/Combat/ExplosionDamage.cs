using System;
using Cinemachine;
using UnityEngine;

public class ExplosionDamage : MonoBehaviour
{
    public float DamageTickRate = 2f;
    
    public CameraShakeData CameraShakeData;
    
    [Header("Damage and blocked")]
    [SerializeField] private GameplayEffect DamageEffect;
    [SerializeField] private GameplayEffect BlockedEffect;
    [SerializeField] private string ObjectToDamage;
    
    [Header("Sound")]
    [SerializeField] private SFXDefinition def;

    private bool isIn = false;
    private float tickTimer = 0f;
    private MakeInvincible invincible;
    private GameplayEffectManager effectmanager;
    private AttributeSystem attributeSystem;
    private DefenseState defense;
    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void OnDisable()
    {
        isIn = false;
    }

    private void OnEnable()
    {
        SoundFXManager.instance.PlaySoundRandomPitch(def.sfx, transform, def.volume, def.minPitch, def.maxPitch);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(ObjectToDamage))
        {
            invincible = other.gameObject.GetComponent<MakeInvincible>();
            effectmanager = other.gameObject.GetComponent<GameplayEffectManager>();
            attributeSystem = other.gameObject.GetComponent<AttributeSystem>();
            defense = other.gameObject.GetComponent<DefenseState>();
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
                HitResult result = defense.ResolveHit();

                switch (result)
                {
                    case HitResult.PerfectParry:
                        break;
                    case HitResult.Blocked:
                        CameraShakeManager.instance.CameraShake(CameraShakeData, impulseSource);
                        effectmanager.ApplyEffect(BlockedEffect, attributeSystem);
                        break;
                    case HitResult.FullDamage:
                        CameraShakeManager.instance.CameraShake(CameraShakeData, impulseSource);
                        effectmanager.ApplyEffect(DamageEffect, attributeSystem);
                        break;
                }
            }
        }
    }

    
}
