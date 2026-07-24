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
    
    [Header("Combo effects")]
    [SerializeField] private List<GameplayEffect> gameplayEffects;
    
    [Header("Camera shakes")]
    [SerializeField] private List<CameraShakeEntry> cameraShakes;
    
    private int ComboNumber = 0;
    private CinemachineImpulseSource impulseSource;
    private Dictionary<CameraShakeState, CameraShakeData> cameraShakeDict;

    void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        
        cameraShakeDict = new Dictionary<CameraShakeState, CameraShakeData>();

        foreach (var entry in cameraShakes)
        {
            cameraShakeDict[entry.State] = entry.Data;
        }
    }

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

    private void DamageEnemy(Collider other)
    {
        BehaviorGraphAgent agent = other.gameObject.GetComponent<BehaviorGraphAgent>();
        agent.BlackboardReference.GetVariableValue("AttackingState", out AttackingState state);

        if (state == AttackingState.Defend)
        {
            EnemyCounter.Invoke();
            agent.BlackboardReference.SetVariableValue("AttackingState", AttackingState.CounterAttack);
            CameraShakeManager.instance.CameraShake(cameraShakeDict[CameraShakeState.gotBlocked], impulseSource);
            return;
        }
        
        GameplayEffectManager effectmanager = other.gameObject.GetComponent<GameplayEffectManager>();
        AttributeSystem attributeSystem = other.gameObject.GetComponent<AttributeSystem>();

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

}
