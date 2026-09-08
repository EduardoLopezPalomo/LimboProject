using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputIconSwitcher : MonoBehaviour
{
    public List<Image> imagenes;
    [SerializeField] private List<Sprite> gamepadSprites;
    [SerializeField] private List<Sprite> keyboardSprites;

    private static readonly List<InputIconSwitcher> instanciasActivas = new List<InputIconSwitcher>();

    private void OnEnable()
    {
        instanciasActivas.Add(this);

        PlayerInput playerInput = BuscarPlayerInput();
        OnControlsChanged(playerInput);
    }

    private void OnDisable()
    {
        instanciasActivas.Remove(this);
    }

    private PlayerInput BuscarPlayerInput()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<PlayerInput>() : null;
    }

    // NUEVO: método estático al que llama el "notifier" del jugador
    public static void NotificarCambioEsquema(PlayerInput input)
    {
        foreach (var instancia in instanciasActivas)
        {
            instancia.OnControlsChanged(input);
        }
    }

    private void OnControlsChanged(PlayerInput input)
    {
        if (input == null) return;

        bool esGamepad = input.currentControlScheme.Contains("Gamepad");
        List<Sprite> spritesAUsar = esGamepad ? gamepadSprites : keyboardSprites;

        for (int i = 0; i < imagenes.Count; i++)
        {
            if (i < spritesAUsar.Count)
                imagenes[i].sprite = spritesAUsar[i];
        }
    }
}