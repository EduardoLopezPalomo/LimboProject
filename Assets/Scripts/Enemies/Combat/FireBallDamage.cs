using UnityEngine;

public class FireBallDamage : MonoBehaviour
{
    
    [SerializeField] private GameplayEffect DamageEffect;
    [SerializeField] private string ObjectToDamage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(ObjectToDamage))
        {
            MakeInvincible invincible = collision.gameObject.GetComponent<MakeInvincible>();
            GameplayEffectManager effectmanager = collision.gameObject.GetComponent<GameplayEffectManager>();
            AttributeSystem attributeSystem = collision.gameObject.GetComponent<AttributeSystem>();
            if (effectmanager == null || attributeSystem == null)
            {
                Debug.Log("effectmanager, attributesystem or makeInvincible is null");
                return;
            }
            if (invincible != null && invincible.IsInvincible())
            {
                return;
            }
            
            effectmanager.ApplyEffect(DamageEffect, attributeSystem);
           
        }
    }

}
