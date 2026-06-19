using System.Collections;
using StarterAssets;
using UnityEngine;

public class KnockBack : MonoBehaviour
{
    public float Distance;
    public float KnockBackTime;
    
    private MakeInvincible makeInvincible;
    private ThirdPersonController personcontroller;
    private CharacterController charactercontroller;
    private Animator animator;
    private Vector3 direction;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        charactercontroller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        makeInvincible = GetComponent<MakeInvincible>();
        personcontroller = GetComponent<ThirdPersonController>();
    }

    
    public void StartKnockBack()
    {
        animator.SetTrigger("KnockBack");
        direction = -transform.forward;
        StartCoroutine(KnockbackMovement());
    }
    
        
    private IEnumerator KnockbackMovement()
    {
        makeInvincible.SetInvincible(true);
        personcontroller.SetInputAviable(false);

        float elapsed = 0f;
        float speed = Distance / KnockBackTime;

        while (elapsed < KnockBackTime)
        {
            charactercontroller.Move(direction * speed * Time.deltaTime);
            elapsed += Time.deltaTime;

            yield return null;
        }

        makeInvincible.SetInvincible(false);
        personcontroller.SetInputAviable(true);
    }
}
