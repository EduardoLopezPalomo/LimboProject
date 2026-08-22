using System;
using UnityEngine;

[Serializable]
public enum BossPhase
{
    Phase1, 
    Phase2,
    Phase3
}
public class PhaseController : MonoBehaviour
{
    [Header("Life percentage for phase change")] 
    [SerializeField] private float LifeForPhase2 = 0.66f;
    [SerializeField] private float LifeForPhase3 = 0.33f;
    
    private AttributeSystem attributeSystem;
    private EnemyGraphBridge enemyGraphBridge;
    private string healthAttributeName = "EnemyHealth";
    private float maxHealth;

    public BossPhase CurrentPhase { get; private set; } = BossPhase.Phase1;

    private void Awake()
    {
        attributeSystem = GetComponent<AttributeSystem>();
        
        maxHealth = attributeSystem.GetAttributeMaxValue(healthAttributeName);
        enemyGraphBridge = GetComponent<EnemyGraphBridge>();
    }

    private void OnEnable()
    {
        EnemyHealthResponse.OnEnemyHurt += HandleAttributeChanged;
    }
    
    private void OnDisable()
    {
        EnemyHealthResponse.OnEnemyHurt -= HandleAttributeChanged;
    }
    
    private void HandleAttributeChanged()
    {
        float newValue = attributeSystem.GetAttributeValue(healthAttributeName);
        float ratio = newValue / maxHealth;
        BossPhase newPhase = CurrentPhase;

        if (ratio <= LifeForPhase3)
        {
            newPhase = BossPhase.Phase3;
        }
        else if (ratio <= LifeForPhase2)
        {
            newPhase = BossPhase.Phase2;
        }
        else
        {
            newPhase = BossPhase.Phase1;
        }

        if (newPhase != CurrentPhase)
        {
            CurrentPhase = newPhase;
            enemyGraphBridge.SetChangePhase();
        }
    }
}
