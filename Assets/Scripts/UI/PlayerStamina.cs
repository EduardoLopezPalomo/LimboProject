using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    [SerializeField] private AbilityHolder abilityHolder;
    [SerializeField] private string abilityName = "Dash";
    [SerializeField] private Image fillImage;
    [SerializeField] private float interpGoingDown;
    
    private float MaxCooldown;
    private float displayedCooldown = 0f;
    private GameplayAbilitySpec abilitySpec;
    
    private void Start()
    {
        if (abilityHolder == null)
            abilityHolder = GetComponentInParent<AbilityHolder>();

        abilitySpec = abilityHolder.GetAbility(abilityName);
        MaxCooldown = abilitySpec.ability.Cooldown;
    }
    
    private void LateUpdate()
    {
        UpdateFill();
    }
    
    void UpdateFill()
    {
        float current = abilitySpec.GetCooldown();
        
        if (current > displayedCooldown)
            displayedCooldown = Mathf.Lerp(displayedCooldown, current, Time.deltaTime * interpGoingDown);
        else
            displayedCooldown = current;
        
        fillImage.fillAmount = Mathf.Clamp01(( MaxCooldown - displayedCooldown) / MaxCooldown);
    }

}
