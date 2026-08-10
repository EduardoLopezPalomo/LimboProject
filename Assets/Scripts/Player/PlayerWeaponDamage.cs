using System;
using System.Collections.Generic;
using Cinemachine;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public enum CameraShakeState
{
    attack1,
    attack2,
    attack3,
    attack4,
    hitShield,
    gotBlocked
}

[Serializable]
public class CameraShakeEntry
{
    public CameraShakeState State;
    public CameraShakeData Data;
}

public class PlayerWeaponDamage : MonoBehaviour
{
    public UnityEvent EnemyCounter;
    
    public int sweepSteps = 5;
    public bool drawGizmos = true;

    [Header("Combo effects")]
    [SerializeField] private List<GameplayEffect> gameplayEffects;

    [Header("Camera shakes")]
    [SerializeField] private List<CameraShakeEntry> cameraShakes;
    
    private BoxCollider weaponCollider;
    private int ComboNumber = 0;
    private int enemyLayerMask;
    private CinemachineImpulseSource impulseSource;
    private Dictionary<CameraShakeState, CameraShakeData> cameraShakeDict;
    
    private bool isActive = false;
    private HashSet<Collider> alreadyHit = new HashSet<Collider>();

    private Vector3 lastCenter;
    private Quaternion lastRotation;
    private bool hasLastFrame = false;

    void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        weaponCollider = GetComponent<BoxCollider>();
        enemyLayerMask = 1 << LayerMask.NameToLayer("EnemyLayer");
        cameraShakeDict = new Dictionary<CameraShakeState, CameraShakeData>();

        foreach (var entry in cameraShakes)
        {
            cameraShakeDict[entry.State] = entry.Data;
        }
    }
    
    void FixedUpdate()
    {
        if (!isActive) return;

        Vector3 currentCenter = weaponCollider.transform.TransformPoint(weaponCollider.center);
        Quaternion currentRotation = weaponCollider.transform.rotation;
        Vector3 halfExtents = Vector3.Scale(weaponCollider.size * 0.5f, weaponCollider.transform.lossyScale);

        if (!hasLastFrame)
        {
            CheckOverlap(currentCenter, halfExtents, currentRotation);
        }
        else
        {
            for (int i = 1; i <= sweepSteps; i++)
            {
                float t = i / (float)sweepSteps;
                Vector3 pos = Vector3.Lerp(lastCenter, currentCenter, t);
                Quaternion rot = Quaternion.Slerp(lastRotation, currentRotation, t);
                CheckOverlap(pos, halfExtents, rot);
            }
        }

        lastCenter = currentCenter;
        lastRotation = currentRotation;
        hasLastFrame = true;
    }

    void CheckOverlap(Vector3 center, Vector3 halfExtents, Quaternion rotation)
    {
        Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, enemyLayerMask, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
        {
            if (!alreadyHit.Contains(hit))
            {
                alreadyHit.Add(hit);
                DamageEnemy(hit);
            }
        }
    }

    /*
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Shield"))
        {
            AbilityHolder abilityHolder = GetComponentInParent<AbilityHolder>();
            Animator animator = GetComponentInParent<Animator>();
            if (abilityHolder != null && animator != null)
            {
                animator.SetBool("InCombo", false);
                animator.SetTrigger("HitShield");
                abilityHolder.CancelAbility("LightAttack");
            }
            CameraShakeManager.instance.CameraShake(cameraShakeDict[CameraShakeState.hitShield], impulseSource);
            return;
        }
        if (other.gameObject.layer == LayerMask.NameToLayer("EnemyLayer"))
        {
            DamageEnemy(other);
        }
    }
    */

    private void DamageEnemy(Collider other)
    {
        BehaviorGraphAgent agent = other.gameObject.GetComponentInParent<BehaviorGraphAgent>();
        agent.BlackboardReference.GetVariableValue("AttackingState", out AttackingState state);
        RotateAlongPlayer rotateAlongPlayer = other.gameObject.GetComponentInParent<RotateAlongPlayer>();

        if (state == AttackingState.Defend && rotateAlongPlayer.isBlocking)
        {
            EnemyCounter.Invoke();
            agent.BlackboardReference.SetVariableValue("AttackingState", AttackingState.CounterAttack);
            CameraShakeManager.instance.CameraShake(cameraShakeDict[CameraShakeState.gotBlocked], impulseSource);
            
            CharacterVFX vfx = other.gameObject.GetComponentInParent<CharacterVFX>();
            CharacterSFX sfx = other.gameObject.GetComponentInParent<CharacterSFX>();
            if (vfx != null && sfx != null)
            {
                vfx.SpawnVFX(VFXType.Counter);
                sfx.SpawnSFX(SFXType.Counter);
            }
            return;
        }

        GameplayEffectManager effectmanager = other.gameObject.GetComponentInParent<GameplayEffectManager>();
        AttributeSystem attributeSystem = other.gameObject.GetComponentInParent<AttributeSystem>();

        if (ComboNumber < 0 || ComboNumber >= gameplayEffects.Count)
        {
            Debug.LogWarning("the combo is " + ComboNumber + "But the list doent have that number");
            return;
        }

        GameplayEffect effect = gameplayEffects[ComboNumber];
        MakeComboCameraShake(ComboNumber);

        if (effect == null)
        {
            Debug.LogWarning("the combo number has no effect on the player weapon");
            return;
        }

        effectmanager.ApplyEffect(effect, attributeSystem);
    }

    private void MakeComboCameraShake(int combo)
    {
        switch (combo)
        {
            case 0:
                CameraShakeManager.instance.CameraShake(cameraShakeDict[CameraShakeState.attack1], impulseSource);
                return;
            case 1:
                CameraShakeManager.instance.CameraShake(cameraShakeDict[CameraShakeState.attack2], impulseSource);
                return;
            case 2:
                CameraShakeManager.instance.CameraShake(cameraShakeDict[CameraShakeState.attack3], impulseSource);
                return;
            case 3:
                CameraShakeManager.instance.CameraShake(cameraShakeDict[CameraShakeState.attack4], impulseSource);
                return;
        }
    }

    public void SetComboNumber(int value)
    {
        ComboNumber = value;
    }
    
    public void ActivateHitbox()
    {
        isActive = true;
        alreadyHit.Clear();
        hasLastFrame = false;
    }

    public void DeactivateHitbox()
    {
        isActive = false;
        hasLastFrame = false;
    }
    
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || weaponCollider == null) return;
        Gizmos.color = isActive ? Color.red : Color.green;
        Matrix4x4 rotMatrix = Matrix4x4.TRS(
            weaponCollider.transform.TransformPoint(weaponCollider.center),
            weaponCollider.transform.rotation,
            weaponCollider.transform.lossyScale);
        Gizmos.matrix = rotMatrix;
        Gizmos.DrawWireCube(Vector3.zero, weaponCollider.size);
    }

}
