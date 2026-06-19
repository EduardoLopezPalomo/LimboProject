using System;
using UnityEngine;
using UnityEngine.AI;

public class RotateAlongPlayer : MonoBehaviour
{
    public Transform player;
    public float rotationSpeedAlongPlayer = 10f;
    public float selfRotationSpeed = 60f;
    public float speed = 1.5f;
    
    private bool StopRotation = true;
    private int direction = 1;
    private float currentAngle;
    private float currentRadius;
    private float previusSpeed;
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
                Mathf.Sin(rad) * currentRadius,
                0f,
                Mathf.Cos(rad) * currentRadius
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
        agent.speed = previusSpeed;
        if (agent.isOnNavMesh)
        {
            agent.ResetPath();   
        }
    }

    public void StartRotating()
    {
        StopRotation = false;
        Animator.SetBool("SideStep", true);
        
        previusSpeed = agent.speed;
        agent.speed = speed;
        
        direction = UnityEngine.Random.value < 0.5f ? -1 : 1;
        float animatorDirection = direction == -1 ? 1 : 0;
        Animator.SetFloat("MotionSpeed", animatorDirection);
        
        Vector3 toPlayer = transform.position - player.position;
        toPlayer.y = 0f;
        currentAngle = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;
        currentRadius = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(player.position.x, 0, player.position.z));
    }
}
