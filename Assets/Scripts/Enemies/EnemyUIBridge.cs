using UnityEngine;

public class EnemyUIbridge : MonoBehaviour
{
    public EnemyHealthBar healthBar;
    
    public void ShowHealth()
    {
        if (healthBar != null && !healthBar.gameObject.activeSelf)
        {
            healthBar.gameObject.SetActive(true);
        }
    }
    
}
