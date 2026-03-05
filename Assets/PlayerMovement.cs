using UnityEngine;
using UnityEngine.InputSystem; // Nuevo Input System

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    private CharacterController controller;
    private Vector2 moveInput; // eje X/Y del teclado

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Este método será llamado por Player Input
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void Update()
    {
        // Convertimos el input 2D en movimiento 3D
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);

        // Ajustar movimiento relativo a cámara (isométrica)
        move = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0) * move;

        controller.Move(move * speed * Time.deltaTime);
    }
}