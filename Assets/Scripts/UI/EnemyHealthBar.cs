using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private AttributeSystem attributeSystem;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
    [Header("Health Bar")]
    [SerializeField] private string healthAttributeName = "EnemyHealth";
    [SerializeField] private Image hFillImage;
    [SerializeField] private Image hOverFillImage;
    [Header("Resistance")]
    [SerializeField] private string resistanceAttributeName = "BlockResistance";
    [SerializeField] private Image rFillImage;
    [SerializeField] private Image rRecoveryImage;
    [SerializeField] private float recoveryDuration = 4f;
    
    private float maxHealth;
    private float overFillMaxhealth;
    private float maxResistance;
    private Camera mainCamera;
    
    private bool isRecovering;
    private float recoveryStartValue;
    private float recoveryTargetValue;
    private float recoveryTimer;
    private float lastKnownResistance;
    
    private void OnEnable()
    {
        CounterAttackSpec.OnResistanceConsumed += HandleResistanceConsumed;
        ResistanceResponse.OnResistanceBack += HandleResistanceBack;
    }
    private void OnDisable()
    {
        CounterAttackSpec.OnResistanceConsumed -= HandleResistanceConsumed;
        ResistanceResponse.OnResistanceBack -= HandleResistanceBack;
    }

    private void Start()
    {
        mainCamera = Camera.main;

        if (attributeSystem == null)
            attributeSystem = GetComponentInParent<AttributeSystem>();
        
        maxHealth = attributeSystem.GetAttributeValue(healthAttributeName);
        overFillMaxhealth = attributeSystem.GetAttributeMaxValue(healthAttributeName);
        maxResistance = attributeSystem.GetAttributeMaxValue(resistanceAttributeName);
        
        lastKnownResistance = attributeSystem.GetAttributeValue(resistanceAttributeName);
        rFillImage.fillAmount = Mathf.Clamp01(lastKnownResistance / maxResistance);
        rRecoveryImage.fillAmount = rFillImage.fillAmount;
        
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        Debug.Log(attributeSystem.GetAttributeValue(resistanceAttributeName));
        UpdatePosition();
        UpdateFill();
        UpdateOverFill();
        UpdateResistanceRecovery(Time.deltaTime);
        FaceCamera();
    }

    void UpdateFill()
    {
        if (maxHealth <= 0) return;
        
        float current = attributeSystem.GetAttributeValue(healthAttributeName);

        hFillImage.fillAmount = Mathf.Clamp01(current / maxHealth);
    }

    void UpdateOverFill()
    {
        float current = attributeSystem.GetAttributeValue(healthAttributeName);
        if (maxHealth <= 0 || current <= maxHealth)
        {
            hOverFillImage.fillAmount = 0f;
            return;
        }
        
        current -= maxHealth;
        hOverFillImage.fillAmount = Mathf.Clamp01(current / (overFillMaxhealth - maxHealth));
    }
    
    void UpdateResistanceRecovery(float deltaTime)
    {
        if (maxResistance <= 0 || !isRecovering) return;

        recoveryTimer += deltaTime;
        float t = Mathf.Clamp01(recoveryTimer / recoveryDuration);

        float value = Mathf.Lerp(recoveryStartValue, recoveryTargetValue, t);
        rRecoveryImage.fillAmount = Mathf.Clamp01(value / maxResistance);

        if (t >= 1f)
        {
            rFillImage.fillAmount = rRecoveryImage.fillAmount;
            lastKnownResistance = recoveryTargetValue; 
            isRecovering = false;
        }
    }
    
    void HandleResistanceConsumed(GameObject sender)
    {
        if (sender != attributeSystem.gameObject) return;
        if (maxResistance <= 0) return;

        float current = attributeSystem.GetAttributeValue(resistanceAttributeName); 
        float previous = lastKnownResistance;
        
        float droppedFill = Mathf.Clamp01(current / maxResistance);
        rFillImage.fillAmount = droppedFill;
        rRecoveryImage.fillAmount = droppedFill;
        
        recoveryStartValue = current;
        recoveryTargetValue = previous;
        recoveryTimer = 0f;
        isRecovering = true;
    }
    
    void HandleResistanceBack(GameObject sender)
    {
        if (sender != attributeSystem.gameObject) return;
        if (maxResistance <= 0) return;

        float current = attributeSystem.GetAttributeValue(resistanceAttributeName);
        float fillAmount = Mathf.Clamp01(current / maxResistance);

        rFillImage.fillAmount = fillAmount;
        rRecoveryImage.fillAmount = fillAmount;
    }

    void UpdatePosition()
    {
        if (followTarget != null)
            transform.position = followTarget.position + offset;
    }

    void FaceCamera()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
    }
}
