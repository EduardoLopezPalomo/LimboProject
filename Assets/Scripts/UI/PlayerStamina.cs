using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    [SerializeField] private AttributeSystem attributeSystem;
    [SerializeField] private string resistanceAttributeName = "Resistance";
    [SerializeField] private Image rFillImage;
    [SerializeField] private Image rRecoveryImage;
    
    private float recoveryDuration;
    private float maxResistance;
    
    private bool isRecovering;
    private float recoveryStartValue;
    private float recoveryTargetValue;
    private float recoveryTimer;
    private float lastKnownResistance;
    private float trackedResistance;
    
    
    private void OnEnable()
    {
        ResistanceResponse.OnResistanceConsumed += HandleResistanceConsumed;
        ResistanceResponse.OnResistanceBack += HandleResistanceBack;
    }
    private void OnDisable()
    {
        ResistanceResponse.OnResistanceConsumed -= HandleResistanceConsumed;
        ResistanceResponse.OnResistanceBack -= HandleResistanceBack;
    }
    
    private void Start()
    {
        maxResistance = attributeSystem.GetAttributeMaxValue(resistanceAttributeName);
        
        lastKnownResistance = attributeSystem.GetAttributeValue(resistanceAttributeName);
        trackedResistance = lastKnownResistance;
        rFillImage.fillAmount = Mathf.Clamp01(lastKnownResistance / maxResistance);
        rRecoveryImage.fillAmount = rFillImage.fillAmount;
    }
    
    private void LateUpdate()
    {
        UpdateResistanceRecovery(Time.deltaTime);
    }
    
    void UpdateResistanceRecovery(float deltaTime)
    {
        if (maxResistance <= 0 || !isRecovering) return;

        recoveryTimer += deltaTime;
        float t = Mathf.Clamp01(recoveryTimer / recoveryDuration);

        float value = Mathf.Lerp(recoveryStartValue, recoveryTargetValue, t);
        rRecoveryImage.fillAmount = Mathf.Clamp01(value / maxResistance);
        
        trackedResistance = attributeSystem.GetAttributeValue(resistanceAttributeName);
        
        if (t >= 1f)
        {
            rFillImage.fillAmount = rRecoveryImage.fillAmount;
            lastKnownResistance = recoveryTargetValue; 
            isRecovering = false;
        }
    }
    
    void HandleResistanceConsumed(GameObject sender, float duration)
    {
        if (sender != attributeSystem.gameObject) return;
        if (maxResistance <= 0) return;

        float current = attributeSystem.GetAttributeValue(resistanceAttributeName);
        float consumedAmount = trackedResistance - current;

        rFillImage.fillAmount = Mathf.Clamp01(current / maxResistance);

        float currentGhostValue = rRecoveryImage.fillAmount * maxResistance;
        recoveryStartValue = Mathf.Clamp(currentGhostValue - consumedAmount, 0f, maxResistance);
        
        recoveryTargetValue = lastKnownResistance;
        recoveryDuration = duration;
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

}
