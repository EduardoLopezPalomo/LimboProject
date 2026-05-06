using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;


[Serializable, GeneratePropertyBag]
[NodeDescription(name: "TravelAlongPoints", story: "[Self] travels along [Route]", category: "Action", id: "01acac7ea890bf526ee84aed2543ef1c")]
public partial class TravelAlongPointsAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<List<GameObject>> Route;
    [SerializeReference] public BlackboardVariable<bool> Backwards = new(false);
    [SerializeReference] public BlackboardVariable<float> Speed = new(3f);
    [SerializeReference] public BlackboardVariable<float> DistanceThreshold = new(0.2f);
    [SerializeReference] public BlackboardVariable<string> AnimatorSpeedParam = new("Speed");
    
    private NavMeshAgent m_NavMeshAgent;
    private Animator m_Animator;
    [CreateProperty] private Vector3 m_CurrentTarget;
    [CreateProperty] private float m_OriginalStoppingDistance = -1f;
    [CreateProperty] private float m_OriginalSpeed = -1f;
    private float m_CurrentSpeed;
    [CreateProperty] private int m_CurrentPatrolPoint = 0;
    
    protected override Status OnStart()
    {
        if (Self.Value == null)
        {
            LogFailure("No agent assigned.");
            return Status.Failure;
        }

        if (Route.Value == null || Route.Value.Count == 0)
        {
            LogFailure("No waypoints to patrol assigned.");
            return Status.Failure;
        }

        Initialize();
        if(Backwards)
        {
            MoveToPreviusWaypoint();
        }
        else
        {
            MoveToNextWaypoint();
        }
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self.Value == null || Route.Value == null)
        {
            return Status.Failure;
        }
        
        float distance = GetDistanceToWaypoint();
        bool destinationReached = distance <= DistanceThreshold;

        // Check if we've reached the waypoint (ensuring NavMeshAgent has completed path calculation if available)
        if (destinationReached && (m_NavMeshAgent == null || !m_NavMeshAgent.pathPending))
        {
            if(Backwards)
            {
                if (m_CurrentPatrolPoint <= 0)
                {
                    UpdateAnimatorSpeed(0f);
                    return Status.Success;
                }

                MoveToPreviusWaypoint();
            }
            else
            {
                if (m_CurrentPatrolPoint >= Route.Value.Count - 1)
                {
                    UpdateAnimatorSpeed(0f);
                    return Status.Success;
                }
                    
                MoveToNextWaypoint();
            }
            
        }
        else if (m_NavMeshAgent == null) 
        {
            LogFailure("No NavMeshAgent in the agent");
        }

        UpdateAnimatorSpeed();

        return Status.Running;
    }

    protected override void OnEnd()
    {
        UpdateAnimatorSpeed(0f);

        if (m_NavMeshAgent != null)
        {
            if (m_NavMeshAgent.isOnNavMesh)
            {
                m_NavMeshAgent.ResetPath();
            }
            m_NavMeshAgent.speed = m_OriginalSpeed;
            m_NavMeshAgent.stoppingDistance = m_OriginalStoppingDistance;
        }
    }

    protected override void OnDeserialize()
    {
        // If using a navigation mesh, we need to reset default value before Initialize.
        m_NavMeshAgent = Self.Value.GetComponentInChildren<NavMeshAgent>();
        if (m_NavMeshAgent != null)
        {
            if (m_OriginalSpeed >= 0f)
                m_NavMeshAgent.speed = m_OriginalSpeed;
            if (m_OriginalStoppingDistance >= 0f)
                m_NavMeshAgent.stoppingDistance = m_OriginalStoppingDistance;

            m_NavMeshAgent.Warp(Self.Value.transform.position);
        }

        int patrolPoint = m_CurrentPatrolPoint - 1;
        Initialize();
        // During deserialization, bypass PreserveLatestPatrolPoint.
        m_CurrentPatrolPoint = patrolPoint;
    }

    private void Initialize()
    {
        m_Animator = Self.Value.GetComponentInChildren<Animator>();
        m_NavMeshAgent = Self.Value.GetComponentInChildren<NavMeshAgent>();
        if (m_NavMeshAgent != null)
        {
            if (m_NavMeshAgent.isOnNavMesh)
            {
                m_NavMeshAgent.ResetPath();
            }

            m_OriginalSpeed = m_NavMeshAgent.speed;
            m_NavMeshAgent.speed = Speed.Value;
            m_OriginalStoppingDistance = m_NavMeshAgent.stoppingDistance;
            m_NavMeshAgent.stoppingDistance = DistanceThreshold;
            m_NavMeshAgent.autoBraking = false;
        }

        UpdateAnimatorSpeed(0f);

        if(Backwards)
        {
            m_CurrentPatrolPoint = Route.Value.Count - 1;
        }
        else
        {
            m_CurrentPatrolPoint = 0;
        }
    }

    private float GetDistanceToWaypoint()
    {
        if (m_NavMeshAgent != null)
        {
            return m_NavMeshAgent.remainingDistance;
        }

        Vector3 targetPosition = m_CurrentTarget;
        Vector3 agentPosition = Self.Value.transform.position;
        agentPosition.y = targetPosition.y; // Ignore y for distance check.
        return Vector3.Distance(agentPosition, targetPosition);
    }

    private void MoveToNextWaypoint()
    {
        m_CurrentPatrolPoint++;

        m_CurrentTarget = Route.Value[m_CurrentPatrolPoint].transform.position;
        if (m_NavMeshAgent != null)
        {
            m_NavMeshAgent.SetDestination(m_CurrentTarget);
        }
    }

    private void MoveToPreviusWaypoint()
    {
        m_CurrentPatrolPoint--;

        m_CurrentTarget = Route.Value[m_CurrentPatrolPoint].transform.position;
        if (m_NavMeshAgent != null)
        {
            m_NavMeshAgent.SetDestination(m_CurrentTarget);
        }
    }

    private void UpdateAnimatorSpeed(float explicitSpeed = -1f)
    {
        if (m_Animator == null) return;

        float speedValue;

        if (explicitSpeed >= 0f)
        {
            speedValue = explicitSpeed;
        }
        else if (m_NavMeshAgent != null)
        {
            speedValue = m_NavMeshAgent.velocity.magnitude;
        }
        else
        {
            speedValue = m_CurrentSpeed;
        }

        m_Animator.SetFloat(AnimatorSpeedParam.Value, speedValue);
    }
}

