using System;
using UnityEngine;
using UnityEngine.AI;

public class RotateAlongPlayer : MonoBehaviour
{
    public Transform player;
    public float rotationSpeedAlongPlayer = 10f;
    public float selfRotationSpeed = 60f;
    public float speed = 1.5f;
    
    [Header("Wobble (variación individual en la órbita)")]
    public float wobbleAmplitude = 8f;   
    public float wobbleFrequency = 0.4f;
 
    [Header("Frenado dinámico por proximidad a aliados")]
    public float neighborCheckRadius = 3f;
    public float minSafeDistance = 1.5f;

    [HideInInspector] public bool isBlocking = false;

    private bool StopRotation = true;
    private float currentAngle;
    private float currentRadius;
    private float previusSpeed;
    private float wobbleSeed;
    private Slot reservedSlot;
    private NavMeshAgent agent;
    private Animator Animator;
    private GoingToPlayer SlotPosition;
    private EnemyWeaponController weaponController;
    private LayerMask enemyLayer;

    void Start()
    {
        Animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        SlotPosition = GetComponent<GoingToPlayer>();
        weaponController = GetComponent<EnemyWeaponController>();
        enemyLayer = LayerMask.GetMask("EnemyLayer");
        agent.updateRotation = false;
        agent.stoppingDistance = 0f;
        
        wobbleSeed = UnityEngine.Random.value * 100f;
    }

    void Update()
    {
        if (!StopRotation)
        {
            UpdateRadius();
 
            if (reservedSlot != null)
            {
                float targetAngle = reservedSlot.GetAngle(player.position);
                float wobble = Mathf.Sin((Time.time + wobbleSeed) * wobbleFrequency) * wobbleAmplitude;
 
                float slowdown = GetNeighborSlowdownFactor();
                currentAngle = Mathf.MoveTowardsAngle(
                    currentAngle,
                    targetAngle + wobble,
                    rotationSpeedAlongPlayer * slowdown * Time.deltaTime
                );
            }
 
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 targetPosition = player.position + new Vector3(
                Mathf.Sin(rad) * currentRadius,
                0f,
                Mathf.Cos(rad) * currentRadius
            );
 
            agent.SetDestination(targetPosition);
            Turn();
            UpdateAnimatorMovement();
        }
    }
    private void UpdateRadius()
    {
        Vector3 flatPlayerPos = new Vector3(player.position.x, 0f, player.position.z);
        Vector3 flatSelfPos = new Vector3(transform.position.x, 0f, transform.position.z);
        float actualDistance = Vector3.Distance(flatPlayerPos, flatSelfPos);

        if (actualDistance < currentRadius)
        {
            currentRadius = actualDistance;
        }
    }
    
    private float GetNeighborSlowdownFactor()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, neighborCheckRadius, enemyLayer);
        float minDist = float.MaxValue;
 
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
 
            float d = Vector3.Distance(transform.position, hit.transform.position);
            if (d < minDist) minDist = d;
        }
 
        if (minDist < minSafeDistance)
            return Mathf.Clamp01(minDist / minSafeDistance); 
 
        return 1f;
    }

    private void UpdateAnimatorMovement()
    {
        Vector3 worldVelocity = agent.velocity;
        worldVelocity.y = 0f;

        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        float maxSpeed = agent.speed > 0f ? agent.speed : speed;
        float targetVelX = Mathf.Clamp(localVelocity.x / maxSpeed, -1f, 1f);
        float targetVelZ = Mathf.Clamp(localVelocity.z / maxSpeed, -1f, 1f);

        float currentVelX = Animator.GetFloat("VelocityX");
        float currentVelZ = Animator.GetFloat("VelocityZ");

        float smoothVelX = Mathf.Lerp(currentVelX, targetVelX, Time.deltaTime * 10f);
        float smoothVelZ = Mathf.Lerp(currentVelZ, targetVelZ, Time.deltaTime * 10f);

        if (Mathf.Abs(smoothVelX) < 0.01f) smoothVelX = 0f;
        if (Mathf.Abs(smoothVelZ) < 0.01f) smoothVelZ = 0f;

        Animator.SetFloat("VelocityX", smoothVelX);
        Animator.SetFloat("VelocityZ", smoothVelZ);
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
        isBlocking = false;
        Animator.SetBool("IsStrafing", false);
        Animator.SetFloat("VelocityX", 0f);
        Animator.SetFloat("VelocityZ", 0f);
        agent.speed = previusSpeed;
        agent.updateRotation = true;
        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
        
        if (reservedSlot != null)
        {
            SlotsManager.Instance.ReleaseSlot(reservedSlot);
            reservedSlot = null;
        }
    }
    
    public void SetReservedSlot(Slot slot)
    {
        reservedSlot = slot;
    }

    public void StartRotating()
    {
        StopRotation = false;
        isBlocking = true;
        Animator.SetBool("IsStrafing", true);
        weaponController.EquipWeapon();

        previusSpeed = agent.speed;
        agent.speed = speed;

        Vector3 toPlayer = transform.position - player.position;
        toPlayer.y = 0f;
        currentAngle = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;

        if (reservedSlot == null && SlotPosition != null && SlotPosition.GetActualSlot() != Vector3.zero)
        {
            Vector3 targetPosition = SlotPosition.GetActualSlot();
            targetPosition.y = 0f;
            currentRadius = Vector3.Distance(targetPosition, new Vector3(player.position.x, 0, player.position.z));
        }
        else if (reservedSlot != null)
        {
            currentRadius = Vector3.Distance(
                new Vector3(reservedSlot.position.x, 0, reservedSlot.position.z),
                new Vector3(player.position.x, 0, player.position.z)
            );
        }
        else
        {
            currentRadius = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(player.position.x, 0, player.position.z));
        }
    }
}
