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

    [Header("Encounter map blip")]
    [SerializeField, Tooltip("This boss's MapMarker. Authored disabled so the blip means \"the fight is on\" instead of \"there is a boss in this level\"; this component switches it on the first time THIS boss loses health. Leave None to take the MapMarker on this GameObject. If the marker is authored enabled this reveals nothing - that is the always-visible answer, and it needs no flag here.")]
    private MapMarker encounterMarker;

    private AttributeSystem attributeSystem;
    private EnemyGraphBridge enemyGraphBridge;
    private string healthAttributeName = "EnemyHealth";
    private float maxHealth;
    private bool encounterMarkerRevealed;
    private const float HealthEpsilon = 0.0001f;

    public BossPhase CurrentPhase { get; private set; } = BossPhase.Phase1;

    private void Awake()
    {
        attributeSystem = GetComponent<AttributeSystem>();

        // GetAttributeMaxValue reads the attribute's data asset, which is valid the moment this
        // component exists. GetAttributeValue is NOT safe to call here: an attribute's CurrentValue is
        // only computed by AttributeSystem.Awake(), and Unity does not define the order the Awakes of
        // two components on one GameObject run in. Measured in SpaceBoss play mode 11/14/2026, this
        // Awake ran first and read 0. Everything below therefore works off maxHealth: the encounter
        // test that used a baseline remembered here saw 0, read the first hit as a health GAIN, and
        // left the map blip hidden until the second hit.
        maxHealth = attributeSystem.GetAttributeMaxValue(healthAttributeName);
        if (maxHealth <= 0f)
            Debug.LogError($"{name} has no usable {healthAttributeName} attribute (max read as {maxHealth}), so the "
                         + "phase thresholds cannot trigger and the encounter blip can never be revealed. Check the "
                         + "AttributeSystem on this boss and the attribute name this component listens to.", this);

        enemyGraphBridge = GetComponent<EnemyGraphBridge>();

        if (encounterMarker == null)
            encounterMarker = GetComponent<MapMarker>();
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

        // EnemyHealthResponse.OnEnemyHurt is a static, parameterless event: it fires for ANY enemy
        // carrying that response, not only this boss. Only health below what THIS boss started the
        // fight with means "the fight is on", so the reveal cannot be driven off the event alone -
        // and it is compared against maxHealth rather than a remembered previous value, for the
        // Awake-order reason documented in Awake().
        if (newValue < maxHealth - HealthEpsilon)
            RevealEncounterMarker();

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

    /// <summary>
    /// D-13: the boss's MapMarker is authored disabled, so the blip means "the fight is on" instead
    /// of "there is a boss in this level". The signal this component can actually observe is its own
    /// health sitting below the value the boss started the fight with, which is what
    /// <see cref="HandleAttributeChanged"/> now tests. The behavior graph keeps its own
    /// ActiveComponent node on the same marker (authoring value: enabled); both paths only ever switch
    /// it on, so neither can undo the other, and an authored-enabled marker makes this a no-op.
    /// </summary>
    private void RevealEncounterMarker()
    {
        if (encounterMarkerRevealed) return;
        encounterMarkerRevealed = true;

        if (encounterMarker == null)
        {
            Debug.LogError($"{name} lost health but has no MapMarker to reveal: none assigned on PhaseController "
                         + "and none on this GameObject, so the boss never appears on the map. Add a MapMarker "
                         + "or assign the one the blip should use.", this);
            return;
        }

        encounterMarker.enabled = true;
    }
}
