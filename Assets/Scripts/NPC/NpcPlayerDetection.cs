using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NpcPlayerDetection : MonoBehaviour, IConversation
{
    /// <summary>
    /// T-057 A2: the detections that currently see the player.
    ///
    /// This used to be a single <c>static bool PossibleInteraction</c> that any NPC could set true and
    /// that only its own <c>OnTriggerExit</c> could set false. <c>StarterAssetsInputs.OnLightAttack</c>
    /// reads it to suppress the light attack, so the flag had three ways to get stranded at true with
    /// nothing left to clear it: an NPC deactivated while the player stood in its trigger (pooling, or
    /// <c>NPCPool</c> putting it to sleep), a level load while the player stood in a trigger (a static
    /// survives a scene unload and <c>LevelLoader</c> loads single-mode), and one NPC's exit clearing
    /// every other NPC's prompt. Village carries 133 of these colliders sharing the one bool, and
    /// <c>Forest</c> has none at all - so once stranded there, the sword stayed dead for the scene.
    ///
    /// Counting the detections that see the player makes the value self-correcting: <see cref="OnDisable"/>
    /// runs when an NPC is pooled out and when the scene unloads, so the set always drains.
    /// </summary>
    private static readonly HashSet<NpcPlayerDetection> detectingPlayer = new HashSet<NpcPlayerDetection>();

    /// <summary>True while at least one reachable NPC would accept an Interact.</summary>
    public static bool PossibleInteraction => detectingPlayer.Count > 0;

    [SerializeField] private GameObject interactUI;
    [SerializeField] private GameObject npc;
    [SerializeField] private DialogueNPC dialogueNPC;

    private GameObject player;
    private PlayerInput playerInput;
    private Collider playerCollider;
    private Collider ownCollider;
    private bool playerDetected = false;
    private bool reportedMissingPrompt = false;

    /// <summary>T-046 B4: "Interact" used to be looked up by name every frame.</summary>
    private readonly CachedInputAction interactActionCache = new CachedInputAction("Interact");

    /// <summary>T-057: how far outside the trigger footprint the overlap re-check forgives before it
    /// decides the player really left (a teleport can skip OnTriggerExit entirely).</summary>
    private const float SeparationMargin = 0.5f;
    private const float HeightTolerance = 8f;

    private void Awake()
    {
        ownCollider = GetComponent<Collider>();
    }

    void Update()
    {
        if (!playerDetected) return;

        // T-057: OnTriggerExit is NOT guaranteed when the player is moved rather than walked. Village has
        // three TeleportScript colliders that reposition the transform directly, and a detection then
        // keeps playerDetected == true forever. Under the old shared bool some other NPC's next exit
        // cleared it by accident; now that the count is honest, one stale entry would suppress the light
        // attack for as long as that NPC stayed alive. So the overlap is re-verified from the colliders -
        // one AABB test per NPC that currently believes it has the player.
        if (!StillOverlappingPlayer())
        {
            ClearDetection();
            return;
        }

        // T-057 F1: interactUI is unassigned on 66 of the 133 detections shipped in Village, and the
        // next statement is the first thing Unity touches after the early return - it threw
        // UnassignedReferenceException once per frame while the player was in range. The throw also
        // meant the Interact handling below never ran, so those components did nothing at all, and
        // they are redundant: every one of them has a sibling detection on the same NPC that is wired
        // correctly. Keeping the behaviour and reporting it once instead of 60 times a second.
        if (interactUI == null)
        {
            if (!reportedMissingPrompt)
            {
                reportedMissingPrompt = true;
                Debug.LogWarning("NpcPlayerDetection on '" + name + "' has no interactUI assigned, so this " +
                                 "detection stays inert and relies on the sibling detection of the same NPC. " +
                                 "Wire the prompt or delete the redundant collider.", this);
            }
            return;
        }

        detectingPlayer.Add(this);

        // T-046 B5: this used to call SetActive(true) on every frame the player stayed in range, with
        // the object already on. The state is the same; only a real change is worth a call.
        if (!interactUI.activeSelf)
            interactUI.SetActive(true);

        if (playerInput != null && playerInput.currentActionMap != null && playerInput.currentActionMap.name != "UI")
        {
            InputAction interactAction = interactActionCache.Get(playerInput);

            if (interactAction != null && interactAction.WasPressedThisFrame())
            {
                StartDialogue();
            }
        }
    }

    private void StartDialogue()
    {
        if (dialogueNPC == null)
        {
            Debug.LogError("NpcPlayerDetection on '" + name + "' has no DialogueNPC, so it cannot build a dialogue.", this);
            return;
        }

        DialogueInitData data = new DialogueInitData();
        data.lines = dialogueNPC.GetDialogueLines();
        data.npcId = dialogueNPC.NpcId;
        data.characterName = dialogueNPC.NpcName;
        data.voice = dialogueNPC.NpcVoice;
        data.portrait = dialogueNPC.Portrait;
        data.input = playerInput;

        IConversation.RaiseDialogueInit(data);

        playerInput.SwitchCurrentActionMap("UI");
        interactUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerDetected = true;
            player = other.gameObject;
            playerInput = other.GetComponent<PlayerInput>();
            playerCollider = other;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ClearDetection();
        }
    }

    /// <summary>
    /// The single place "the player is in range" is dropped, because there are three ways to lose it:
    /// OnTriggerExit, the overlap re-check in Update, and this object being disabled.
    /// </summary>
    private void ClearDetection()
    {
        playerDetected = false;
        detectingPlayer.Remove(this); // T-057 A2: this NPC left range, not every NPC in the level

        // T-057 candidate 5: the prompt normally goes away with the NPC, but clearing it here also covers
        // the NPC coming back on while the player never actually left, so the only way it reappears is a
        // real detection.
        if (interactUI != null)
        {
            interactUI.SetActive(false);
        }
    }

    private bool StillOverlappingPlayer()
    {
        if (playerCollider == null) return false;   // the player object is gone
        if (ownCollider == null) return true;       // nothing to measure against, keep trusting the event

        // Deliberately looser than a real trigger test, because a false negative here would take the
        // interact prompt away from someone standing right next to the NPC. These triggers are ~1 m tall
        // slabs, so Y is checked with a generous window and only the horizontal footprint decides: a
        // teleport that lands 10 m away still fails this immediately, while jumping or a cobbled step
        // never does.
        Bounds b = ownCollider.bounds;
        b.Expand(new Vector3(SeparationMargin, SeparationMargin, SeparationMargin));
        Vector3 p = playerCollider.bounds.center;

        bool insideFootprint = p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z;
        bool saneHeight = Mathf.Abs(p.y - b.center.y) <= HeightTolerance;

        return insideFootprint && saneHeight;
    }

    /// <summary>
    /// T-057 A2: the other ways the player stops being in range without an OnTriggerExit ever firing -
    /// the NPC is pooled out by PoolManager, put to sleep by NPCPool, or the scene unloads. Without
    /// this the count never drains and the light attack stays suppressed for the rest of the scene.
    /// </summary>
    private void OnDisable()
    {
        ClearDetection();
    }
}