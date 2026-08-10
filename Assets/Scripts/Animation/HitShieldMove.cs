using UnityEngine;

public class HitShieldMove : MonoBehaviour
{
    public float moveDistance = 2f;
    public float moveSpeed = 5f;

    private float moved = 0f;
    private bool movingBack = false;
    private CharacterController controller;
    private KnockBack knockBack;
    
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        knockBack = GetComponent<KnockBack>();
    }
    
    void Update()
    {
        if (movingBack && !knockBack.NotMove)
        {
            float step = moveSpeed * Time.deltaTime;
            controller.Move(-transform.forward * step);

            moved += step;

            if (moved >= moveDistance)
            {
                movingBack = false;
                controller.Move(Vector3.zero);
                moved = 0f;
            }
        }
    }

    public void MoveBackwards()
    {
        movingBack = true;
    }
}
