using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LockOn : MonoBehaviour
{
    public float detectionRadius = 15f;
    public LayerMask enemyLayer;
    public string enemyTag = "Enemy";
    public Transform playerRoot;
    public Camera mainCamera;

    [Header("Visual Effect")] 
    public VFXDefinition VFX;
    
    [HideInInspector] public Transform currentTarget;
    [HideInInspector] public bool isLocked;
    
    private List<Transform> _enemiesInRange = new List<Transform>();
    private PlayerWeaponController weaponController;
    private Animator animator;
    private GameObject vfxGameObject;

    private void OnEnable()
    {
        EnemyGraphBridge.OnDeath += EnemyDeath;
    }

    private void OnDisable()
    {
        EnemyGraphBridge.OnDeath -= EnemyDeath;
    }

    private void Awake()
    {
        weaponController = GetComponent<PlayerWeaponController>();
        animator = GetComponent<Animator>();
    }

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

    private void FindAndLockNearest()
    {
        RefreshEnemiesInRange();

        if (_enemiesInRange.Count == 0)
        {
            ClearLock();
            return;
        }
        
        currentTarget = _enemiesInRange.OrderBy(e => ScreenDistanceFromCenter(e)).First();
        if (vfxGameObject != null)
        {
            VFXManager.Instance.Cancel(vfxGameObject);   
        }
        vfxGameObject = VFXManager.Instance.Play(VFX, currentTarget.position, Quaternion.identity, currentTarget.gameObject);
        animator.SetBool("IsStrafing", true);
        weaponController.EquipWeapon();
        isLocked = true;
    }

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

    public void ClearLock()
    {
        isLocked = false;
        currentTarget = null;
        VFXManager.Instance.Cancel(vfxGameObject);
        vfxGameObject = null;
        animator.SetBool("IsStrafing", false);
        weaponController.UnequipWeapon();
    }

    private void RefreshEnemiesInRange()
    {
        _enemiesInRange.Clear();
        Collider[] hits = Physics.OverlapSphere(playerRoot.position, detectionRadius, enemyLayer);

        foreach (var h in hits)
        {
            if (!string.IsNullOrEmpty(enemyTag) && !h.CompareTag(enemyTag)) continue;
            if (!IsVisibleOnScreen(h.transform)) continue;
            _enemiesInRange.Add(h.transform);
        }
    }

    private bool IsVisibleOnScreen(Transform t)
    {
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(t.position);
        return viewportPos.z > 0 &&
               viewportPos.x > 0.05f && viewportPos.x < 0.95f &&
               viewportPos.y > 0.05f && viewportPos.y < 0.95f;
    }

    private float ScreenDistanceFromCenter(Transform t)
    {
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(t.position);
        return Vector2.Distance(new Vector2(viewportPos.x, viewportPos.y), new Vector2(0.5f, 0.5f));
    }

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
            
            bool stillValid = Vector3.Distance(playerRoot.position, currentTarget.position) <= detectionRadius * 1.3f && currentTarget.gameObject.activeInHierarchy;
            if (!stillValid) FindAndLockNearest();
        }
    }
}

