using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class InputIconSwitcher : MonoBehaviour
{
    [FormerlySerializedAs("imagenes")]
    public List<Image> images;
    [SerializeField] private List<Sprite> gamepadSprites;
    [SerializeField] private List<Sprite> keyboardSprites;

    private static readonly List<InputIconSwitcher> activeInstances = new List<InputIconSwitcher>();

    private void OnEnable()
    {
        if (!activeInstances.Contains(this))
            activeInstances.Add(this);

        PlayerInput playerInput = FindPlayerInput();
        OnControlsChanged(playerInput);
    }

    private void OnDisable()
    {
        activeInstances.Remove(this);
    }

    private void OnDestroy()
    {
        activeInstances.Remove(this);
    }

    private PlayerInput FindPlayerInput()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<PlayerInput>() : null;
    }

    // NUEVO: método estático al que llama el "notifier" del jugador
    public static void NotifySchemeChanged(PlayerInput input)
    {
        for (int i = activeInstances.Count - 1; i >= 0; i--)
        {
            activeInstances[i].OnControlsChanged(input);
        }
    }

    private void OnControlsChanged(PlayerInput input)
    {
        if (input == null) return;

        bool isGamepad = input.currentControlScheme.Contains("Gamepad");
        List<Sprite> spritesToUse = isGamepad ? gamepadSprites : keyboardSprites;

        for (int i = 0; i < images.Count; i++)
        {
            if (i < spritesToUse.Count)
                images[i].sprite = spritesToUse[i];
        }
    }
}