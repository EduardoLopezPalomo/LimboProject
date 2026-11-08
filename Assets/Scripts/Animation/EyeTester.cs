using UnityEngine;

public class EyeTester : MonoBehaviour
{
    [SerializeField] private EyeAnimation eye;

    private void Start()
    {
    Debug.Log("EyeTester iniciado");
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            eye.PlayClose(() => Debug.Log("Ojo cerrado"));
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            eye.PlayOpen(() => Debug.Log("Ojo abierto"));
        }
    }
}