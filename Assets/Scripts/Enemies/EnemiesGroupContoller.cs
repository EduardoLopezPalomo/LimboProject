using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

public class EnemiesGroupContoller : MonoBehaviour
{
    private List<GameObject> enemies = new List<GameObject>();
    void Awake()
    {
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

    void Update()
    {

    }

}
