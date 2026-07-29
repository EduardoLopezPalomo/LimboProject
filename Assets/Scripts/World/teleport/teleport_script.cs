using UnityEngine;
using UnityEngine.SceneManagement;

public class teleport_script : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private bool PartyOn = false;

    private void OnTriggerEnter(Collider other)
    {
              
        if (other.CompareTag("Player"))
        {
            if (PartyOn)
            {
                SceneManager.LoadScene("Party");
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}