using UnityEngine;
using UnityEngine.AI;

public class TokenTimer : MonoBehaviour
{
    public float cooldownTimeMin = 3f;
    public float cooldownTimeMax = 6f;

    [SerializeField] private EnemiesGroupController controller;
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
                // Guarded: a null controller used to throw every frame from Update.
                if (controller != null) controller.ReturnToken();
            }
        }
    }
    
    public void StartCooldown()
    {
        cooldownTime = Random.Range(cooldownTimeMin, cooldownTimeMax);
        isOnCooldown = true;
        timer = 0f;

        if (controller == null)
        {
            controller = EnemiesGroupController.Resolve(gameObject);
            if (controller == null)
            {
                Debug.LogError($"{name}: no {nameof(EnemiesGroupController)} assigned and none found above {gameObject.name} - the attack token will never be returned.", gameObject);
            }
        }
    }
    
    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }
    
}
