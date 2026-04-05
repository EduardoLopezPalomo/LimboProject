using System;
using UnityEngine;

public class RotateAlongPlayer : MonoBehaviour
{
    public Transform player;
    public float rotationSpeedALongPlayer = 10f;
    public float selfRotationSpeed = 60f;
    private bool StopRotation = true;
    private int direction = 1;

    void Update()
    {
        if (!StopRotation)
        {
            transform.RotateAround(player.position, Vector3.up, direction * rotationSpeedALongPlayer * Time.deltaTime);
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
    }

    public void StartRotating()
    {
        StopRotation = false;
        direction = UnityEngine.Random.value < 0.5f ? -1 : 1;
    }
}
