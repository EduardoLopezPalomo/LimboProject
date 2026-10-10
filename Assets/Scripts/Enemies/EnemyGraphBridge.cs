using UnityEngine;
using Unity.Behavior;
using UnityEngine.AI;

/// <summary>
/// EnemyGraphBridge provides a bridge between the enemy's Behavior Graph AI system and other game components.
/// This component allows external systems to control the enemy's state and behavior.
/// </summary>
public class EnemyGraphBridge : MonoBehaviour
{
    /// <summary>
    /// Static event fired when an enemy dies.
    /// Used by other systems (LockOn, combat, etc.) to react to enemy deaths.
    /// </summary>
    public static event System.Action<GameObject> OnDeath;
    
    private BehaviorGraphAgent agent;

    void Awake()
    {
        agent = GetComponent<BehaviorGraphAgent>();
    }

    /// <summary>
    /// Sets the enemy's state to Hurt.
    /// Used when the enemy has taken damage.
    /// </summary>
    public void SetHurt()
    {
        agent.BlackboardReference.GetVariableValue("ActualState", out BarbarianState state); 
        if (state != BarbarianState.Death)
        {
            agent.BlackboardReference.SetVariableValue("ActualState", BarbarianState.Hurt);
        }
    }

    /// <summary>
    /// Sets the enemy's state to Vulnerable.
    /// Used when the enemy is in a state where it can be attacked.
    /// </summary>
    public void SetVulnerable()
    {
        agent.BlackboardReference.GetVariableValue("ActualState", out BarbarianState state); 
        if (state != BarbarianState.Death)
        {
            agent.BlackboardReference.SetVariableValue("AttackingState", AttackingState.Vulnerable);
        }
    }
    
    /// <summary>
    /// Sets the change phase flag.
    /// Used to trigger boss phase transitions.
    /// </summary>
    public void SetChangePhase()
    {
        agent.BlackboardReference.SetVariableValue("ChangePhase", true);
    }

    /// <summary>
    /// Sets the enemy's state to Trap (using shield/trap ability).
    /// </summary>
    public void SetTrap()
    {
        agent.BlackboardReference.GetVariableValue("ActualState", out BarbarianState state); 
        if (state != BarbarianState.Death)
        {
            agent.BlackboardReference.SetVariableValue("ActualState", BarbarianState.Trap);
        }
    }
    
    /// <summary>
    /// Sets the enemy's detection state and marks it as alerted.
    /// Used when the enemy detects the player.
    /// </summary>
    public void SetDetection()
    {
        agent.BlackboardReference.GetVariableValue("ActualState", out BarbarianState state); 
        if (state != BarbarianState.Death)
        {
            agent.BlackboardReference.SetVariableValue("ActualState", BarbarianState.Detection);
            agent.BlackboardReference.SetVariableValue("HasBeenAlerted", true);
        }
    }

    /// <summary>
    /// Sets the enemy to death state and disables all functionality.
    /// </summary>
    public void SetDeath()
    {
        if (agent != null)
        {
            agent.BlackboardReference.SetVariableValue("ActualState", BarbarianState.Death);
        }

        OnDeath?.Invoke(gameObject);
        DisableAllExceptRenderer();
    }
    
    /// <summary>
    /// Disables all enemy functionality except renderer (for death animation).
    /// - Disables NavMeshAgent (AI movement)
    /// - Disables CharacterController (movement)
    /// - Disables all MonoBehaviors
    /// - Enables Animator for death animation
    /// - Cancels death VFX
    /// - Enables SkinnedMeshRenderer for visual appearance
    /// </summary>
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
