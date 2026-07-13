using UnityEngine;

public class PlayerAnimMainMenu : MonoBehaviour
{
    public float MinTime = 2f;
    public float MaxTime = 8f;
    
    private Animator animator;
    private bool wait = false;
    private float randomTime;
    private float actualTime;
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!wait)
        {
            wait = true;
            randomTime = Random.Range(MinTime, MaxTime);
        }
        
        actualTime += Time.deltaTime;

        if (actualTime > randomTime)
        {
            animator.SetTrigger("Movement");
            actualTime = 0;
            wait = false;
        }
    }
}
