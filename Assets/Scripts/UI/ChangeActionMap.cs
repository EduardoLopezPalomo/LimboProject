using UnityEngine;
using UnityEngine.InputSystem;

public class ChangeActionMap : MonoBehaviour
{
    public bool fromStart = false;
    public PlayerInput playerInput;
    
    void Start()
    {
        if (fromStart)
        {
            ChangeToUI();   
        }
    }
    
    public void ChangeToUI()
    {
        playerInput.SwitchCurrentActionMap("UI");
    }
    
    public void ChangeToPlayer()
    {
        playerInput.SwitchCurrentActionMap("Player");
    }
    
}
