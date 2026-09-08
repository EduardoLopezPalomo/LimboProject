using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ChangeControllerImage : MonoBehaviour
{
    public List<Image> Images;

    public List<Sprite> spriteGamepad;
    public List<Sprite> spriteKeyboard;

    private PlayerInput playerInput;

    private void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        playerInput = player.GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        playerInput.onControlsChanged += OnControlsChanged;
        OnControlsChanged(playerInput);
    }

    private void OnDisable()
    {
        playerInput.onControlsChanged -= OnControlsChanged;
    }

    private void OnControlsChanged(PlayerInput input)
    {
        bool isGamepad = input.currentControlScheme == "Gamepad";

        if (isGamepad)
        {
            int i = 0;
            foreach (Image image in Images)
            {
                image.sprite = spriteGamepad[i];
                i++;
            }
        }
        else
        {
            int i = 0;
            foreach (Image image in Images)
            {
                image.sprite = spriteKeyboard[i];
                i++;
            }
        }
    }
}
