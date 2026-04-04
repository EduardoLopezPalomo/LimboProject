using UnityEngine;
using StarterAssets;

public class InputController : MonoBehaviour
{
    private ThirdPersonController controller;
    
    private void Awake()
    {
        controller = GetComponent<ThirdPersonController>();
    }

    public void EnableInput()
    {
        if (controller != null)
        {
            controller.SetInputAviable(true);
        }
    }

    public void DisableInput()
    {
        if (controller != null)
        {
            controller.SetInputAviable(false);
        }
    }
}
