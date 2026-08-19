using System.Collections.Generic;
using System;
using Unity.Behavior;
using UnityEngine;

public class EnemiesGroupContoller : MonoBehaviour
{
    public static event Action<List<GameObject>> OnHideEnemyHud;
    public static event Action<bool> OnBattleStateChanged;
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
            Debug.Log($"[Token] {Time.time:F2}s PEDIDO -> quedan {currentToken}");
            currentToken--;
            return true;
        }
        Debug.Log($"[Token] {Time.time:F2}s DENEGADO -> quedan {currentToken}");
        return false;
    }

    public void ReturnToken()
    {
        currentToken++;
        currentToken = Mathf.Clamp(currentToken, 0, maxTokens);
        Debug.Log($"[Token] {Time.time:F2}s DEVUELTO -> quedan {currentToken}");
    }

    public void AlertGroup(GameObject alerter)
    {
        OnBattleStateChanged?.Invoke(true);
        MusicManager.Instance.StartBattle();
        foreach (var v in enemies)
        {
            if (alerter == v) continue;

            EnemyGraphBridge aux = v.GetComponent<EnemyGraphBridge>();
            aux.SetDetection();
        }
    }

    public void EnemyHasDie(GameObject enemy)
    {
        enemies.Remove(enemy);
        CheckBattleOver();
    }

    public void EnemyEndPersecution()
    {
        CheckBattleOver();
    }

    private void CheckBattleOver()
    {
        foreach (var enemy in enemies)
        {
            BehaviorGraphAgent agent = enemy.GetComponent<BehaviorGraphAgent>();
            BlackboardVariable<BarbarianState> stateVariable;

            if (agent.GetVariable("ActualState", out stateVariable))
            {

                if (stateVariable.Value != BarbarianState.Calm && stateVariable.Value != BarbarianState.Death)
                {
                    return;
                }
            }
        }

        BattleOver();
    }

    private void BattleOver()
    {
        MusicManager.Instance.EndBattle();
        OnHideEnemyHud?.Invoke(enemies);
        OnBattleStateChanged?.Invoke(false);
    }

    //Methods for slots managment


}
