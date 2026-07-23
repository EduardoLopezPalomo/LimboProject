using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    private ThirdPersonController controller;
    public PlayerInput playerInput;
    
    private void Awake()
    {
        controller = GetComponent<ThirdPersonController>();
    }

    public void EnableInput()
    {
        if (controller != null)
        {
            controller.SetInputAviable(true);
            playerInput.ActivateInput();
        }
    }

    public void DisableInput()
    {
        if (controller != null)
        {
            controller.SetInputAviable(false);
            playerInput.DeactivateInput();
        }
    }
}
