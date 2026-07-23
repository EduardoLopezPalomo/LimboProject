using System.Collections.Generic;
using System;
using UnityEngine;

public class EnemyUIbridge : MonoBehaviour
{
    public EnemyHealthBar healthBar;

    void OnEnable()
    {
        EnemiesGroupContoller.OnHideEnemyHud += HideHud;
    }

    void OnDisable()
    {
        EnemiesGroupContoller.OnHideEnemyHud -= HideHud;
    }
    
    public void ShowHealth()
    {
        if (healthBar != null && !healthBar.HealthBar.activeSelf)
        {
            healthBar.HealthBar.SetActive(true);
        }
    }
    
    public void ShowResistance()
    {
        if (healthBar != null && !healthBar.ResistanceBar.activeSelf)
        {
            healthBar.ResistanceBar.SetActive(true);
        }
    }
    
    public void HideHud(List<GameObject> enemies)
    {
        if (healthBar != null)
        {
            healthBar.ResistanceBar.SetActive(false);
            healthBar.HealthBar.SetActive(false);
        }
    }
    
}
