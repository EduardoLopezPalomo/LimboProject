using UnityEngine;
using UnityEngine.AI;

public class TokenTimer : MonoBehaviour
{
    public float cooldownTimeMin = 3f;
    public float cooldownTimeMax = 6f;
    
    private float timer = 0f;
    private float cooldownTime;
    private bool isOnCooldown = false;
    
    void Update()
    {
        if (isOnCooldown)
        {
            timer += Time.deltaTime;
            if (timer >= cooldownTime)
            {
                isOnCooldown = false;
                timer = 0f;
            }
        }
    }
    
    public void StartCooldown()
    {
        cooldownTime = Random.Range(cooldownTimeMin, cooldownTimeMax);
        isOnCooldown = true;
        timer = 0f;
    }
    
    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }
    
}
