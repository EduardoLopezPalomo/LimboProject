using UnityEngine;

public class WeaponDamage : MonoBehaviour
{
    private int ComboNumber = 0;
    [SerializeField] private GameplayEffect DamageEffect;
    [SerializeField] private GameplayEffect FinalDamageEffect;
    [SerializeField] private string ObjectToDamage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Shield") && !gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Hit shield!");
            AbilityHolder abilityHolder = GetComponentInParent<AbilityHolder>();
            Animator animator = GetComponentInParent<Animator>();
            if (abilityHolder != null && animator != null)
            {
                animator.SetBool("SetInCombo", false);
                animator.SetTrigger("HitShield");
                abilityHolder.CancelAbility("LightAttack");   
            }
            return; 
        }
        if (other.gameObject.CompareTag(ObjectToDamage))
        {
            MakeInvincible invincible = other.gameObject.GetComponent<MakeInvincible>();
            GameplayEffectManager effectmanager = other.gameObject.GetComponent<GameplayEffectManager>();
            AttributeSystem attributeSystem = other.gameObject.GetComponent<AttributeSystem>();
            if (effectmanager == null || attributeSystem == null)
            {
                Debug.Log("effectmanager, attributesystem or makeInvincible is null");
                return;
            }
            if (invincible != null && invincible.IsInvincible())
            {
                return;
            }
            if (ComboNumber < 3)
            {
                effectmanager.ApplyEffect(DamageEffect, attributeSystem);
            }
            else
            {
                effectmanager.ApplyEffect(FinalDamageEffect, attributeSystem);
            }
        }
    }

    public void SetComboNumber(int value)
    {
        ComboNumber = value;
    }
}
