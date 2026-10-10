using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// LockOn implements an auto-aiming system that automatically locks onto the nearest enemy.
/// Features:
/// - Radius-based detection (default 15 units)
/// - Visual FX when locking onto enemy
/// - Smooth target switching between enemies
/// - Automatic re-lock when target dies or moves out of range
/// </summary>
public class LockOn : MonoBehaviour
{
    /// <summary>
    /// Detection radius for finding enemies.
    /// Enemies within this radius are considered for lock-on.
    /// Default: 15 units
    /// </summary>
    public float detectionRadius = 15f;
    
    /// <summary>
    /// Layer mask for identifying enemies to lock onto.
    /// </summary>
    public LayerMask enemyLayer;
    
    /// <summary>
    /// Tag of enemies to lock onto.
    /// If empty, all enemies on the enemyLayer are considered.
    /// Default: "Enemy"
    /// </summary>
    public string enemyTag = "Enemy";
    
    /// <summary>
    /// Reference to the player's root transform (usually character controller).
    /// Used as the anchor point for distance calculations.
    /// </summary>
    public Transform playerRoot;
    
    /// <summary>
    /// Reference to the main camera for screen-space calculations.
    /// Used to determine if enemies are visible on screen.
    /// </summary>
    public Camera mainCamera;

    [Header("Visual Effect")] 
    /// <summary>
    /// VFX to play when locking onto an enemy.
    /// Creates visual feedback for the lock-on effect.
    /// </summary>
    public VFXDefinition VFX;
    
    [HideInInspector] public Transform currentTarget;
    [HideInInspector] public bool isLocked;
    
    /// <summary>
    /// List of enemies currently detected within range and visible on screen.
    /// </summary>
    private List<Transform> _enemiesInRange = new List<Transform>();
    
    /// <summary>
    /// Reference to the player's weapon controller.
    /// Used to equip/unequip weapon when locking/unlocking.
    /// </summary>
    private PlayerWeaponController weaponController;
    
    /// <summary>
    /// Reference to the player's animator.
    /// Used to toggle animation state when strafing to target.
    /// </summary>
    private Animator animator;
    
    /// <summary>
    /// GameObject for the visual effect.
    /// Used to cancel VFX when switching targets.
    /// </summary>
    private GameObject vfxGameObject;

    /// <summary>
    /// Called when this component is enabled.
    /// Subscribes to enemy death events to handle target updates.
    /// </summary>
    private void OnEnable()
    {
        EnemyGraphBridge.OnDeath += EnemyDeath;
    }

    /// <summary>
    /// Called when this component is disabled.
    /// Unsubscribes from enemy death events.
    /// </summary>
    private void OnDisable()
    {
        EnemyGraphBridge.OnDeath -= EnemyDeath;
    }

    private void Awake()
    {
        weaponController = GetComponent<PlayerWeaponController>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Toggles lock-on state.
    /// If already locked, clears the lock.
    /// If not locked, attempts to find and lock onto the nearest enemy.
    /// </summary>
    public void ToggleLockOn()
    {
        if (isLocked)
        {
            ClearLock();
        }
        else
        {
            FindAndLockNearest();
        }
    }

    /// <summary>
    /// Finds the nearest enemy and locks onto it.
    /// Processes:
    /// 1. Refreshes list of enemies in range
    /// 2. If no enemies, clears lock
    /// 3. Sorts enemies by screen distance and picks nearest
    /// 4. Plays VFX, equips weapon, sets animation state
    /// </summary>
    private void FindAndLockNearest()
    {
        RefreshEnemiesInRange();

        if (_enemiesInRange.Count == 0)
        {
            ClearLock();
            return;
        }
        
        // Find enemy closest to center of screen
        currentTarget = _enemiesInRange.OrderBy(e => ScreenDistanceFromCenter(e)).First();
        
        // Play lock-on visual effect at target position
        if (vfxGameObject != null)
        {
            VFXManager.Instance.Cancel(vfxGameObject);   
        }
        vfxGameObject = VFXManager.Instance.Play(VFX, currentTarget.position, Quaternion.identity, currentTarget.gameObject);
        
        // Set animation state for strafing movement
        animator.SetBool("IsStrafing", true);
        
        // Equip weapon for attack
        weaponController.EquipWeapon();
        
        isLocked = true;
    }

    /// <summary>
    /// Switches to a different target enemy.
    /// Allows player to switch between enemies using directional input.
    /// Logic:
    /// 1. Refreshes enemy list
    /// 2. If only one enemy, cannot switch
    /// 3. Calculates side (left/right) from camera perspective
    /// 4. Finds enemy on the specified side closest to current target
    /// 5. Updates target and VFX
    /// </summary>
    /// <param name="direction">-1 for left, 1 for right</param>
    public void SwitchTarget(float direction)
    {
        if (!isLocked || currentTarget == null) return;

        RefreshEnemiesInRange();
        if (_enemiesInRange.Count <= 1) return;

        Vector3 camRight = mainCamera.transform.right;
        Vector3 toCurrent = (currentTarget.position - playerRoot.position).normalized;

        Transform best = null;
        float bestScore = float.MaxValue;

        foreach (var e in _enemiesInRange)
        {
            if (e == currentTarget) continue;

            Vector3 toEnemy = (e.position - playerRoot.position).normalized;
            // Calculate which side the enemy is on (left/right from camera)
            float side = Vector3.Dot(camRight, toEnemy - toCurrent); 
            if (Mathf.Sign(side) != Mathf.Sign(direction)) continue;

            float score = Vector3.Angle(toCurrent, toEnemy);
            if (score < bestScore)
            {
                bestScore = score;
                best = e;
            }
        }

        if (best != null)
        {
            currentTarget = best;
            VFXManager.Instance.Cancel(vfxGameObject);
            vfxGameObject = VFXManager.Instance.Play(VFX, currentTarget.position, Quaternion.identity, currentTarget.gameObject);
        }
    }

    /// <summary>
    /// Clears the lock-on state.
    /// Sets isLocked to false, nullifies target, cancels VFX, 
    /// unequips weapon, and resets animation state.
    /// </summary>
    public void ClearLock()
    {
        isLocked = false;
        currentTarget = null;
        VFXManager.Instance.Cancel(vfxGameObject);
        vfxGameObject = null;
        animator.SetBool("IsStrafing", false);
        weaponController.UnequipWeapon();
    }

    /// <summary>
    /// Refreshes the list of enemies in range.
    /// Uses Physics.OverlapSphere to find enemies, then filters by:
    /// - Tag (if set)
    /// - Visibility on screen
    /// </summary>
    private void RefreshEnemiesInRange()
    {
        _enemiesInRange.Clear();
        
        // Find all colliders in the detection radius
        Collider[] hits = Physics.OverlapSphere(playerRoot.position, detectionRadius, enemyLayer);

        foreach (var h in hits)
        {
            // Check if enemy has correct tag (if tag is set)
            if (!string.IsNullOrEmpty(enemyTag) && !h.CompareTag(enemyTag)) continue;
            
            // Check if enemy is visible on screen
            if (!IsVisibleOnScreen(h.transform)) continue;
            
            _enemiesInRange.Add(h.transform);
        }
    }

    /// <summary>
    /// Checks if a transform is visible on the screen.
    /// Uses viewport coordinates:
    /// - z > 0: object is in front of camera
    /// - x: 5% to 95%: horizontally visible
    /// - y: 5% to 95%: vertically visible
    /// </summary>
    /// <param name="t">Transform to check</param>
    /// <returns>True if visible on screen</returns>
    private bool IsVisibleOnScreen(Transform t)
    {
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(t.position);
        return viewportPos.z > 0 &&
               viewportPos.x > 0.05f && viewportPos.x < 0.95f &&
               viewportPos.y > 0.05f && viewportPos.y < 0.95f;
    }

    /// <summary>
    /// Calculates the distance from the center of the screen in viewport coordinates.
    /// Used for sorting enemies by proximity to center (nearest to center = best target).
    /// </summary>
    /// <param name="t">Transform to measure</param>
    /// <returns>Distance from center (0 = center, higher = edges)</returns>
    private float ScreenDistanceFromCenter(Transform t)
    {
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(t.position);
        return Vector2.Distance(new Vector2(viewportPos.x, viewportPos.y), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Called when an enemy dies.
    /// If the dead enemy was the current target, sets target to null.
    /// </summary>
    /// <param name="e">The destroyed enemy object</param>
    private void EnemyDeath(GameObject e)
    {
        if (currentTarget == null) return;
        
        if (e == currentTarget.gameObject)
        {
            currentTarget = null;
        }
    }

    private void Update()
    {
        if (isLocked)
        {
            if (currentTarget == null)
            {
                FindAndLockNearest();
                return;
            }
            
            // Check if target is still valid:
            // - Within detection radius (with buffer)
            // - Still active in scene
            bool stillValid = Vector3.Distance(playerRoot.position, currentTarget.position) <= detectionRadius * 1.3f && currentTarget.gameObject.activeInHierarchy;
            
            if (!stillValid) FindAndLockNearest();
        }
    }
}

