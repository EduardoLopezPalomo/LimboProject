using UnityEngine;

public class WeaponDamage : MonoBehaviour
{
    [SerializeField] private GameplayEffect DamageEffect;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnTriggerEnter(Collider other)
    {
        GameplayEffectManager effectmanager = other.gameObject.GetComponent<GameplayEffectManager>();
        AttributeSystem attributeSystem = other.gameObject.GetComponent<AttributeSystem>();
        if (effectmanager != null && attributeSystem != null)
        {
            effectmanager.ApplyEffect(DamageEffect, attributeSystem);
        }
        //Debug.Log("Damage applied to " + other.gameObject.name);
    }
}
