using System.Collections;
using StarterAssets;
using UnityEngine;

public class KnockBack : MonoBehaviour
{
    public float KnockBackOffsetTime = 2f;

    [HideInInspector] public bool NotMove = false;
    
    private bool CanKnockBack = true;
    private MakeInvincible makeInvincible;
    private AbilityHolder abilityHolder;
    private Animator animator;
    private Vector3 direction;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        makeInvincible = GetComponent<MakeInvincible>();
        abilityHolder = GetComponent<AbilityHolder>();
    }

    public void StartKnockBackCounter()
    {
        if (!CanKnockBack) return;
        NotMove = false;
        animator.SetTrigger("KnockBack");
        if (abilityHolder.HasAbility("LightAttack"))
        {
            abilityHolder.CancelAbility("LightAttack");   
        }
        
        StartCoroutine(Timer(true));
    }

    
    public void StartKnockBack()
    {
        if (!CanKnockBack) return;
        NotMove = false;
        animator.SetTrigger("KnockBack");
        abilityHolder.CancelAllAbility();
        StartCoroutine(Timer(false));
    }

    public void KnockBackWithoutInv()
    {
        if (!CanKnockBack) return;
        NotMove = true;
        animator.SetTrigger("KnockBack");
        abilityHolder.CancelAllAbility();
    }
    
        
    private IEnumerator Timer(bool counter)
    {
        if (CompareTag("Player") && !counter)
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
