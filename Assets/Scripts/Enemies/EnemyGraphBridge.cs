using UnityEngine;
using Unity.Behavior;
using UnityEngine.AI;

public class EnemyGraphBridge : MonoBehaviour
{
    public static event System.Action<GameObject> OnDeath;
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
    
    public void SetChangePhase()
    {
        agent.BlackboardReference.SetVariableValue("ChangePhase", true);
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

        OnDeath?.Invoke(gameObject);
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
        
        agent.BlackboardReference.GetVariableValue("VFXInstance", out GameObject vfx);
        if (vfx != null)
        {
            VFXManager.Instance.Cancel(vfx);
        }

        foreach (var skin in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            skin.enabled = true;
        }
    }
}
