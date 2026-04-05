using UnityEngine;

public class HitShieldMove : MonoBehaviour
{
    public float moveDistance = 2f;
    public float moveSpeed = 5f;

    private float moved = 0f;
    private bool movingBack = false;
    private CharacterController controller;
    
    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }
    
    void Update()
    {
        if (movingBack)
        {
            float step = moveSpeed * Time.deltaTime;
            controller.Move(-transform.forward * step);

            moved += step;

            if (moved >= moveDistance)
            {
                movingBack = false;
                moved = 0f;
            }
        }
    }

    public void MoveBackwards()
    {
        movingBack = true;
    }
}
