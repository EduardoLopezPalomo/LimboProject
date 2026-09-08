using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputIconSwitcher : MonoBehaviour
{
    public List<Image> imagenes;
    [SerializeField] private List<Sprite> gamepadSprites;
    [SerializeField] private List<Sprite> keyboardSprites;

    private PlayerInput playerInput;

    private void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerInput = player.GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        if (playerInput != null)
            playerInput.onControlsChanged += OnControlsChanged;

        OnControlsChanged(playerInput);
    }

    private void OnDisable()
    {
        if (playerInput != null)
            playerInput.onControlsChanged -= OnControlsChanged;
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