using System;
using UnityEngine;
using UnityEngine.AI;

public class RotateAlongPlayer : MonoBehaviour
{
    public Transform player;
    public float rotationSpeedAlongPlayer = 10f;
    public float selfRotationSpeed = 60f;
    private float orbitRadius = 5f;
    
    private bool StopRotation = true;
    private int direction = 1;
    private float currentAngle;
    private NavMeshAgent agent;
    private Animator Animator;
    
    void Start()
    {
        Animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.stoppingDistance = 0f;
    }
    
    void Update()
    {
        if (!StopRotation)
        {
            currentAngle += direction * rotationSpeedAlongPlayer * Time.deltaTime;

            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 targetPosition = player.position + new Vector3(
                Mathf.Sin(rad) * orbitRadius,
                0f,
                Mathf.Cos(rad) * orbitRadius
            );

            agent.SetDestination(targetPosition);
            Turn();
        }
    }

    private void Turn()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0;
        
        Quaternion rotation = Quaternion.LookRotation(direction);
        
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, selfRotationSpeed * Time.deltaTime);
    }
    
    public void StopRotating()
    {
        StopRotation = true;
        Animator.SetBool("SideStep", false);
        if (agent.isOnNavMesh)
        {
            agent.ResetPath();   
        }
    }

    public void StartRotating()
    {
        StopRotation = false;
        Animator.SetBool("SideStep", true);
        direction = UnityEngine.Random.value < 0.5f ? -1 : 1;
        float animatorDirection = direction == -1 ? 1 : 0;
        Animator.SetFloat("MotionSpeed", animatorDirection);
        orbitRadius = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(player.position.x, 0, player.position.z));
    }
}
