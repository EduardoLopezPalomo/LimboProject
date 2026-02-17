using UnityEngine;

public class RootMotionController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public void EnableAnimRootMotion()
    {
        animator.applyRootMotion = true;
    }

    public void DisableAnimRootMotion()
    {
        animator.applyRootMotion = false;
    }
}
