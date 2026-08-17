using Cinemachine;
using UnityEngine;

public class LightingDamage : MonoBehaviour
{
    public CameraShakeData CameraShakeData;
    
    [SerializeField] private GameplayEffect DamageEffect;
    [SerializeField] private GameplayEffect BlockedEffect;
    [SerializeField] private string ObjectToDamage;
    
    private CinemachineImpulseSource impulseSource;
    
    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(ObjectToDamage))
        {
            MakeInvincible invincible = other.gameObject.GetComponent<MakeInvincible>();
            GameplayEffectManager effectmanager = other.gameObject.GetComponent<GameplayEffectManager>();
            AttributeSystem attributeSystem = other.gameObject.GetComponent<AttributeSystem>();
            DefenseState defense = other.gameObject.GetComponent<DefenseState>();
            if (effectmanager == null || attributeSystem == null)
            {
                Debug.Log("effectmanager, attributesystem or makeInvincible is null");
                return;
            }
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
