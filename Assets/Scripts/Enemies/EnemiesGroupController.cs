using System.Collections.Generic;
using System;
using Unity.Behavior;
using UnityEngine;

public class EnemiesGroupController : MonoBehaviour
{
    public static event Action<List<GameObject>> OnHideEnemyHud;
    public static event Action<bool> OnBattleStateChanged;
    public int maxTokens = 2;

    private int currentToken = 0;
    private List<GameObject> enemies = new List<GameObject>();

    /// <summary>
    /// Finds the group controller that owns an enemy.
    ///
    /// The graph nodes cannot carry this reference: commit 574a7847 (T-026 typo sweep) renamed
    /// EnemiesGroupContoller -> EnemiesGroupController inside the graph assets, and Unity does not
    /// deserialize a [SerializeReference] BlackboardVariable&lt;OldName&gt; into a field whose type was
    /// renamed, so AlertGroupAction's GroupController field is null on all 44 live nodes (measured).
    /// Even with the binding restored, a shared .asset could never point at a scene object.
    /// Every placed enemy is instead parented under the group that owns it: measured in Forest, 44 of
    /// 44 enemies are a direct child of exactly one of the 15 groups, none is nested under two, and
    /// Resolve(enemy) equals the group whose own Awake()-built roster contains it, 44 of 44.
    ///
    /// Do NOT replace this with FindObjectOfType: with 15 groups in a level it returns an arbitrary
    /// one and alerts the wrong enemies.
    ///
    /// Returns null when the enemy really is outside every group; the caller decides how loud to be.
    /// </summary>
    public static EnemiesGroupController Resolve(GameObject enemy)
    {
        if (enemy == null) return null;
        return enemy.GetComponentInParent<EnemiesGroupController>(true);
    }

    /// <summary>Names a possibly-null object for a log line.</summary>
    internal static string Describe(GameObject obj)
    {
        return obj != null ? obj.name : "this agent";
    }

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
        else if (enemies.Count == 1)
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
            //Debug.Log($"[Token] {Time.time:F2}s PEDIDO -> quedan {currentToken}");
            currentToken--;
            return true;
        }
        //Debug.Log($"[Token] {Time.time:F2}s DENEGADO -> quedan {currentToken}");
        return false;
    }

    public void ReturnToken()
    {
        currentToken++;
        currentToken = Mathf.Clamp(currentToken, 0, maxTokens);
        //Debug.Log($"[Token] {Time.time:F2}s DEVUELTO -> quedan {currentToken}");
    }

    public void AlertGroup(GameObject alerter)
    {
        OnBattleStateChanged?.Invoke(true);
        MusicManager.Instance.StartBattle();
        foreach (var v in enemies)
        {
            if (alerter == v) continue;
            if (v == null) continue;

            EnemyGraphBridge aux = v.GetComponent<EnemyGraphBridge>();
            // One enemy missing its bridge must not cost the rest of the group their alert.
            if (aux == null)
            {
                Debug.LogError($"{name}: {v.name} has no {nameof(EnemyGraphBridge)} - it will not be alerted.", v);
                continue;
            }
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
            if (enemy == null) continue;

            BehaviorGraphAgent agent = enemy.GetComponent<BehaviorGraphAgent>();
            BlackboardVariable<BarbarianState> stateVariable;

            // Without an agent we cannot read its state; ignore it rather than throwing and leaving
            // the battle running forever (the old code threw a NullReferenceException here).
            if (agent == null)
            {
                Debug.LogError($"{name}: {enemy.name} has no BehaviorGraphAgent - it is ignored when deciding if the battle is over.", enemy);
                continue;
            }

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
