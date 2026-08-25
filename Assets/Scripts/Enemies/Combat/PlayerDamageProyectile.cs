using System.Buffers;
using Unity.Behavior;
using UnityEngine;

public class PlayerDamageProyectile : MonoBehaviour
{
    [SerializeField] private GameplayEffect DamageEffect;
    [SerializeField] private GameplayEffect BlockedEffect;
    [SerializeField] private string ObjectToDamage;
    
    [Header("Sound")]
    [SerializeField] private SFXDefinition def;
    
    private void OnEnable()
    {
        if (def == null) return;
        
        SoundFXManager.instance.PlaySoundRandomPitch(def.sfx, transform, def.volume, def.minPitch, def.maxPitch);
    }
    

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(ObjectToDamage))
        {
            
            GameplayEffectManager effectmanager = other.gameObject.GetComponent<GameplayEffectManager>();
            AttributeSystem attributeSystem = other.gameObject.GetComponent<AttributeSystem>();
            
            if (effectmanager == null || attributeSystem == null)
            {
                Debug.Log("effectmanager, attributesystem or makeInvincible is null");
                
                return;
            }
            
            BehaviorGraphAgent agent = other.gameObject.GetComponent<BehaviorGraphAgent>();
            agent.BlackboardReference.GetVariableValue("AttackingState", out AttackingState state);
            RotateAlongPlayer rotateAlongPlayer = other.gameObject.GetComponentInParent<RotateAlongPlayer>();

            if (state == AttackingState.Defend && rotateAlongPlayer.isBlocking)
            {
                effectmanager.ApplyEffect(BlockedEffect, attributeSystem);
            }
            else
            {
                effectmanager.ApplyEffect(DamageEffect, attributeSystem);
            }
        }
    }
}
