using System;
using UnityEngine;

[CreateAssetMenu(fileName = "KillGoal", menuName = "Quests/Goals/Kill")]
public class KillGoal : Goals
{
    public override void Init()
    {
        base.Init();
        EnemyGraphBridge.OnDeath += EnemyDied;
    }

    private void EnemyDied(GameObject go)
    {
        currentAmount++;
        Evaluate();
    }

    public override void Completed()
    {
        base.Completed();
        EnemyGraphBridge.OnDeath -= EnemyDied;
    }
}
