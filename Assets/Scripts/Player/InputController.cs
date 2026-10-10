using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;

/// <summary>
/// InputController handles enabling and disabling input for the player controller.
/// It acts as a bridge between the game's input system and the StarterAssets ThirdPersonController.
/// </summary>
public class InputController : MonoBehaviour
{
    /// <summary>
    /// Reference to the ThirdPersonController component on this GameObject.
    /// Used to control input availability.
    /// </summary>
    private ThirdPersonController controller;
    
    /// <summary>
    /// Reference to the PlayerInput component from StarterAssets.
    /// Handles the actual input system activation.
    /// </summary>
    public PlayerInput playerInput;
    
    private void Awake()
    {
        // Find the ThirdPersonController component attached to this object
        controller = GetComponent<ThirdPersonController>();
    }

    /// <summary>
    /// Enables input for the player controller.
    /// This allows the player to receive input from the game's input system.
    /// </summary>
    public void EnableInput()
    {
        if (controller != null)
        {
            controller.SetInputAviable(true);
            playerInput.ActivateInput();
        }
    }

    /// <summary>
    /// Disables input for the player controller.
    /// This prevents the player from receiving input, useful for:
    /// - Menu states
    /// - Sleep states
    /// - Death states
    /// </summary>
    public void DisableInput()
    {
        if (controller != null)
        {
            controller.SetInputAviable(false);
            playerInput.DeactivateInput();
        }
    }
}
