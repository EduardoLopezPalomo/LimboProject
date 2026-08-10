using UnityEngine;
using Unity.Behavior;
using UnityEngine.AI;

public class EnemyGraphBridge : MonoBehaviour
{
    private BehaviorGraphAgent agent;

    void Awake()
    {
        agent = GetComponent<BehaviorGraphAgent>();
    }

    public void SetHurt()
    {
        agent.BlackboardReference.GetVariableValue("ActualState", out BarbarianState state); 
        if (state != BarbarianState.Death)
        {
            agent.BlackboardReference.SetVariableValue("ActualState", BarbarianState.Hurt);
        }
    }

    public void SetVulnerable()
    {
        agent.BlackboardReference.GetVariableValue("ActualState", out BarbarianState state); 
        if (state != BarbarianState.Death)
        {
            agent.BlackboardReference.SetVariableValue("AttackingState", AttackingState.Vulnerable);
        }
    }

    public void SetTrap()
    {
        agent.BlackboardReference.GetVariableValue("ActualState", out BarbarianState state); 
        if (state != BarbarianState.Death)
        {
            agent.BlackboardReference.SetVariableValue("ActualState", BarbarianState.Trap);
        }
    }
    
    public void SetDetection()
    {
        agent.BlackboardReference.GetVariableValue("ActualState", out BarbarianState state); 
        if (state != BarbarianState.Death)
        {
            agent.BlackboardReference.SetVariableValue("ActualState", BarbarianState.Detection);
            agent.BlackboardReference.SetVariableValue("HasBeenAlerted", true);
        }
    }

    public void SetDeath()
    {
        if (agent != null)
        {
            agent.BlackboardReference.SetVariableValue("ActualState", BarbarianState.Death);
        }
        DisableAllExceptRenderer();
    }
    
    void DisableAllExceptRenderer()
    {
        var navMesh = GetComponent<NavMeshAgent>();
        if (navMesh != null) navMesh.enabled = false;

        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        foreach (var behaviour in GetComponentsInChildren<MonoBehaviour>())
        {
            behaviour.enabled = false;
        }
        
        var animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool("IsDeath", true);
        }

        foreach (var skin in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            skin.enabled = true;
        }
    }
}
