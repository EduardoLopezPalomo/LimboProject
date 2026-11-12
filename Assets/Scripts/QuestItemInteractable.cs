using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class QuestItemInteractable : MonoBehaviour
{
    public static event Action<string> OnItemInteracted;
    public string itemId;
    [FormerlySerializedAs("actualQuest")]
    public Quest currentQuest;
    [FormerlySerializedAs("actualGoal")]
    public Goals currentGoal;

    public GameplayAbility abilityToUnlock;

    [Header("Referencias")]
    [SerializeField] private GameObject interactUI;

    private bool playerInside = false;
    private PlayerInput playerInput;
    private AbilityHolder abilityHolder;
    private MapMarker mapMarker;
    private BoxCollider boxCollider;

    private void Awake()
    {
        mapMarker = GetComponent<MapMarker>();
        if (mapMarker == null)
            Debug.LogError($"{name} has a QuestItemInteractable but no MapMarker on the same GameObject, so this quest objective never appears on the map. Add one (Static / MissionPoint).", this);
        else
            mapMarker.enabled = false;
        boxCollider = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        if (currentGoal == null) return;
        interactUI.SetActive(false);
        HandleGoalsChanged(null);
    }

    void OnEnable()
    {
        QuestManager.OnGoalChange += HandleGoalsChanged;
        interactUI.SetActive(false);
        HandleGoalsChanged(null);
    }

    void OnDisable() => QuestManager.OnGoalChange -= HandleGoalsChanged;

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
        return currentQuest != null && QuestManager.instance.GetCurrentQuest() == currentQuest.questId && !currentGoal.completed;
    }

    private void Interact()
    {
        OnItemInteracted?.Invoke(itemId);

        if (interactUI != null) interactUI.SetActive(false);

        if (abilityToUnlock != null && abilityHolder != null)
        {
            abilityHolder.AddAbility(abilityToUnlock);
            NotificationManager.Instance.Enqueue(new NotificationData(NotificationType.AbilityUnlocked, abilityToUnlock.Name));
        }

        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        playerInput = other.GetComponent<PlayerInput>();
        abilityHolder = other.GetComponent<AbilityHolder>();
        if (interactUI != null && CanInteract()) interactUI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
        if (interactUI != null) interactUI.SetActive(false);
    }

    private void HandleGoalsChanged(HashSet<string> ids)
    {
        bool isObjective = QuestManager.IsCurrentGoal(itemId);
        bool showGoal = isObjective && !currentGoal.completed;

        if (mapMarker != null)
        {
            mapMarker.enabled = isObjective;
            mapMarker.SetAsGoal(showGoal);
        }

        boxCollider.enabled = showGoal;
    }

    [ContextMenu("Create Id")]
    public void CreateId()
    {
        itemId = "Item" + Guid.NewGuid().ToString();
    }
}