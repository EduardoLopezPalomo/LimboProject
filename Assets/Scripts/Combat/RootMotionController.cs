using UnityEngine;

public class RootMotionController : MonoBehaviour
{
    private Animator animator;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void EnableAnimRootMotion()
    {
        animator.applyRootMotion = true;
    }

    public void DisableAnimRootMotion()
    {
        animator.applyRootMotion = false;
    }
}
