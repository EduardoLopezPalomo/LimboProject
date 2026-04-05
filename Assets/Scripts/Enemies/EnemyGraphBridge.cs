using UnityEngine;
using Unity.Behavior;

public class EnemyGraphBridge : MonoBehaviour
{
    private BehaviorGraphAgent agent;

    void Awake()
    {
        agent = GetComponent<BehaviorGraphAgent>();
    }

    public void SetHurt()
    {
        if (agent != null)
        {
            agent.BlackboardReference.SetVariableValue("ActualState", BarbarianState.Hurt);
        }
    }

    public void SetTrap()
    {
        if (agent != null)
        {
            agent.BlackboardReference.SetVariableValue("ActualState", BarbarianState.Trap);
        }
    }
}
