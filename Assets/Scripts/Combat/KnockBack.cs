using System.Collections;
using StarterAssets;
using UnityEngine;

public class KnockBack : MonoBehaviour
{
    public float KnockBackOffsetTime = 2f;
    
    private bool CanKnockBack = true;
    private MakeInvincible makeInvincible;
    private AbilityHolder abilityHolder;
    private Animator animator;
    private Vector3 direction;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        makeInvincible = GetComponent<MakeInvincible>();
        abilityHolder = GetComponent<AbilityHolder>();
    }

    
    public void StartKnockBack()
    {
        if (!CanKnockBack) return;
        
        animator.SetTrigger("KnockBack");
        if (abilityHolder.HasAbility("LightAttack"))
        {
            abilityHolder.CancelAbility("LightAttack");   
        }
        if (abilityHolder.HasAbility("BowAttack"))
        {
            abilityHolder.CancelAbility("BowAttack");   
        }
        if (abilityHolder.HasAbility("FireAttack"))
        {
            abilityHolder.CancelAbility("FireAttack");   
        } ;
        StartCoroutine(Timer());
    }
    
        
    private IEnumerator Timer()
    {
        if (CompareTag("Player"))
        {
            makeInvincible.SetInvincible(true);   
        }

        CanKnockBack = false;
        float elapsed = 0f;

        while (elapsed < KnockBackOffsetTime)
        {
            elapsed += Time.deltaTime;

            yield return null;
        }
        CanKnockBack = true;
        if (CompareTag("Player"))
        {
            makeInvincible.SetInvincible(false);   
        }
    }
}
