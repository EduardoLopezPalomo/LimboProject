using UnityEngine;

public class WeaponDamage : MonoBehaviour
{
    [SerializeField] private GameplayEffect DamageEffect;
    [SerializeField] private string ObjectToDamage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == ObjectToDamage)
        {
            GameplayEffectManager effectmanager = other.gameObject.GetComponent<GameplayEffectManager>();
            AttributeSystem attributeSystem = other.gameObject.GetComponent<AttributeSystem>();
            if (effectmanager != null && attributeSystem != null)
            {
                effectmanager.ApplyEffect(DamageEffect, attributeSystem);
            }
        }
    }
}
