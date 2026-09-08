using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControlSchemeNotifier : MonoBehaviour
{
    // Este nombre es especial: el Input System lo llama automáticamente
    // porque el PlayerInput está configurado en modo "Send Messages"
    public void OnControlsChanged(PlayerInput input)
    {
        InputIconSwitcher.NotificarCambioEsquema(input);
    }
}