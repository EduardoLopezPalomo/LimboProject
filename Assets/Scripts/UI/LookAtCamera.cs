using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
 
    void LateUpdate()
    {
        transform.forward = Camera.main.transform.forward;
    }
}
