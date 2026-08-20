using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    public GameObject popUp;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            popUp.SetActive(true);
        }
    }
}
