using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

public class EnemiesGroupContoller : MonoBehaviour
{
    public int maxTokens = 2;
    
    private int currentToken = 0;
    private List<GameObject> enemies = new List<GameObject>();
    
    void Awake()
    {
        currentToken = maxTokens;
        // Find all child objects with the "Enemy" tag and add them to the list
        foreach (Transform child in transform)
        {
            if (child.gameObject.CompareTag("Enemy"))
            {
                enemies.Add(child.gameObject);
            }
        }
        if (enemies.Count > 1)
        {
            for (int i = 0; i < enemies.Count - 1; i++)
            {
                BehaviorGraphAgent agent = enemies[i].GetComponent<BehaviorGraphAgent>();
                if (agent != null)
                {
                    agent.SetVariableValue("GroupState", GroupState.Group);
                }
            }
        }
        else
        {
            BehaviorGraphAgent agent = enemies[0].GetComponent<BehaviorGraphAgent>();
            if (agent != null)
            {
                agent.SetVariableValue("GroupState", GroupState.Alone);
            }
        }
    }
    
    public bool RequestToken()
    {
        if (currentToken > 0)
        {
            currentToken--;
            return true;
        }
        return false;
    }
    
    public void ReturnToken()
    {
        currentToken++;
        currentToken = Mathf.Clamp(currentToken, 0, maxTokens);
    }

    public void AlertGroup(GameObject alerter)
    {
        foreach (var v in enemies)
        {
            if(alerter == v) continue;
            
            EnemyGraphBridge aux = v.GetComponent<EnemyGraphBridge>();
            aux.SetDetection();
        }
    }
    //Methods for slots managment
    
    
}
