using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.Video;

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
    [Header("Animación")]
    [SerializeField] private string interactAnimationTrigger = "Taking Item";
     private Animator playerAnimator;
    [Header("Cinematica Opcional")]
    [SerializeField] private VideoPlayer cinematicPlayer;
    [SerializeField] private VideoClip cinematicClip;
    [Header("Eliminar objeto")]
    [SerializeField] private GameObject objectToRemove;
    [SerializeField] private float removeDelay = 0.5f;

    private bool isInteracting = false;

    private bool playerInside = false;
    private PlayerInput playerInput;
    private AbilityHolder abilityHolder;
    private MapMarker mapMarker;
    private BoxCollider boxCollider;
   

    /// <summary>T-046 B4: "Interact" used to be looked up by name every frame while the player stood in the trigger.</summary>
    private readonly CachedInputAction interactActionCache = new CachedInputAction("Interact");

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
        if (!playerInside || playerInput == null || isInteracting || !CanInteract()) return;
        InputAction interactAction = interactActionCache.Get(playerInput);
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
        if (isInteracting) return;
        StartCoroutine(InteractRoutine());
    }

    private IEnumerator InteractRoutine()
    {
        isInteracting = true;
        if (interactUI != null) interactUI.SetActive(false);

        // 1) Cinemática (solo si hay VideoPlayer asignado)
        if (cinematicPlayer != null)
            yield return PlayCinematic();

        // 2) Animación del jugador
        if (!string.IsNullOrEmpty(interactAnimationTrigger) && playerAnimator != null)
            playerAnimator.SetTrigger(interactAnimationTrigger);

        // 3) Interacción
        OnItemInteracted?.Invoke(itemId);

        if (abilityToUnlock != null && abilityHolder != null)
        {
            abilityHolder.AddAbility(abilityToUnlock);
            NotificationManager.Instance.Enqueue(new NotificationData(NotificationType.AbilityUnlocked, abilityToUnlock.Name));
        }

        // 4) Eliminar objeto (el asignado, o este si no hay ninguno)
        if (removeDelay > 0f) yield return new WaitForSeconds(removeDelay);
        GameObject target = objectToRemove != null ? objectToRemove : gameObject;
        target.SetActive(false);
    }

    private IEnumerator PlayCinematic()
    {
        bool finished = false;
        VideoPlayer.EventHandler onEnd = _ => finished = true;
        VideoPlayer.ErrorEventHandler onError = (_, msg) =>
        {
            Debug.LogError($"Error en la cinemática: {msg}", this);
            finished = true;
        };
        cinematicPlayer.loopPointReached += onEnd;
        cinematicPlayer.errorReceived += onError;

        cinematicPlayer.gameObject.SetActive(true);
        if (cinematicClip != null) cinematicPlayer.clip = cinematicClip;
        cinematicPlayer.isLooping = false;
        cinematicPlayer.Prepare();

        while (!cinematicPlayer.isPrepared && !finished) yield return null;
        if (!finished)
        {
            cinematicPlayer.Play();
            while (!finished) yield return null;
        }

        cinematicPlayer.loopPointReached -= onEnd;
        cinematicPlayer.errorReceived -= onError;
        cinematicPlayer.Stop();
        cinematicPlayer.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        playerInput = other.GetComponent<PlayerInput>();
        abilityHolder = other.GetComponent<AbilityHolder>();
        playerAnimator = other.GetComponentInChildren<Animator>();
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