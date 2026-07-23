using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private AttributeSystem attributeSystem;
    [SerializeField] private string healthAttributeName = "PlayerHealth";
    [SerializeField] private Image fillImage;
    [SerializeField] private Image overFillImage;
    
    private float maxHealth;
    private float overFillMaxhealth;
    
    private void Start()
    {
        if (attributeSystem == null)
            attributeSystem = GetComponentInParent<AttributeSystem>();
        
        maxHealth = attributeSystem.GetAttributeValue(healthAttributeName);
        overFillMaxhealth = attributeSystem.GetAttributeMaxValue(healthAttributeName);
    }
    
    private void LateUpdate()
    {
        UpdateFill();
        UpdateOverFill();
    }
    
    void UpdateFill()
    {
        float current = attributeSystem.GetAttributeValue(healthAttributeName);

        if (maxHealth <= 0) return;

        fillImage.fillAmount = Mathf.Clamp01(current / maxHealth);
    }

    void UpdateOverFill()
    {
        float current = attributeSystem.GetAttributeValue(healthAttributeName);
        if (maxHealth <= 0 || current <= maxHealth)
        {
            overFillImage.fillAmount = 0f;
            return;
        }
        
        current -= maxHealth;
        overFillImage.fillAmount = Mathf.Clamp01(current / (overFillMaxhealth - maxHealth));
    }
}
