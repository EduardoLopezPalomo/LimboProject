using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class QuestItemInteractable : MonoBehaviour
{
    public static event Action<string> OnItemInteracted;
    public string itemId;
    public Quest actualQuest;
    
    [Header("Referencias")]
    [SerializeField] private GameObject interactUI;

    private bool playerInside = false;
    private PlayerInput playerInput;
 
    private void Update()
    {
        if (!playerInside || playerInput == null || !CanInteract()) return;

        InputAction interactAction = playerInput.currentActionMap.FindAction("Interact");
        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            Interact();
        }
    }
    
    private bool CanInteract()
    {
        return actualQuest != null && QuestManager.instance.GetActualQuest() == actualQuest.questId;
    }

    private void Interact()
    {
        OnItemInteracted?.Invoke(itemId);

        if (interactUI != null) interactUI.SetActive(false);
        
        gameObject.SetActive(false);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        playerInput = other.GetComponent<PlayerInput>();
        if (interactUI != null && CanInteract()) interactUI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
        if (interactUI != null) interactUI.SetActive(false);
    }
    
    [ContextMenu("Create Id")]
    public void CreateId()
    {
        itemId = "Item" + Guid.NewGuid().ToString();
    }
}