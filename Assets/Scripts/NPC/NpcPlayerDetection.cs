using UnityEngine;

public class NpcPlayerDetection : MonoBehaviour
{
    public float detectionAngle = 0.6f;
   
    [SerializeField] private GameObject interactUI;
    private GameObject player;
    private bool playerDetected = false;

    void Update()
    {
        if(playerDetected)
        {
            Vector3 direction = (transform.root.position - player.transform.position).normalized;
            
            float dot = Vector3.Dot(player.transform.forward, direction);
          
            if (dot > detectionAngle)
            {
                interactUI.SetActive(true);
            }
            else
            {
                interactUI.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerDetected = true;
            player = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerDetected = false;
            interactUI.SetActive(false);
        }
    }
}
