using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    public float recoverValue = 0.1f;
    
    [SerializeField] private AttributeSystem attributeSystem;
    [SerializeField] private string resistanceAttributeName = "Resistance";
    [SerializeField] private Image rFillImage;
    [SerializeField] private Image rRecoveryImage;
    
    private float maxResistance;
    
    private void Start()
    {
        maxResistance = attributeSystem.GetAttributeMaxValue(resistanceAttributeName);
        
        float currentResistance = attributeSystem.GetAttributeValue(resistanceAttributeName);
        rFillImage.fillAmount = Mathf.Clamp01(currentResistance / maxResistance);
        rRecoveryImage.fillAmount = rFillImage.fillAmount;
    }
    
    private void LateUpdate()
    {
        UpdateResistanceFill();
        UpdateResistanceRecovery();
    }
    void UpdateResistanceFill()
    {
        if (maxResistance <= 0) return;
 
        float current = attributeSystem.GetAttributeValue(resistanceAttributeName);
        rFillImage.fillAmount = Mathf.Clamp01(current / maxResistance);
    }
    
    void UpdateResistanceRecovery()
    {
        if (maxResistance <= 0) return;
 
        float target = rFillImage.fillAmount;
        float step = recoverValue / maxResistance;
 
        rRecoveryImage.fillAmount = Mathf.MoveTowards(rRecoveryImage.fillAmount, target, step);
    }

}
