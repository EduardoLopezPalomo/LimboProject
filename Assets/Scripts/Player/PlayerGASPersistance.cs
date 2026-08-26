using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerAttributeCache
{
    public static Dictionary<string, float> Values;
    public static bool HasData;
}

public static class PlayerAbilitiesCache
{
    public static GameplayAbility EquippedAbility1;
    public static GameplayAbility EquippedAbility2;
    public static bool HasData;
}

public class PlayerGASPersistance : MonoBehaviour
{
    private AttributeSystem attributeSystem;
    private AbilityHolder abilityHolder;

    private void Awake()
    {
        attributeSystem = GetComponent<AttributeSystem>();
        abilityHolder = GetComponent<AbilityHolder>();
    }

    public void SaveState()
    {
        SaveAbilities();
        SaveAttributes();
    }
    
    public void RestoreState()
    {
        RestoreAbilities();
        RestoreAttributes();
    }

    private void RestoreAttributes()
    {
        if (PlayerAttributeCache.HasData)
        {
            attributeSystem.ApplyValues(PlayerAttributeCache.Values, false);
        }
    }

    private void SaveAttributes()
    {
        PlayerAttributeCache.Values = attributeSystem.CaptureValues();
        PlayerAttributeCache.HasData = true;
    }

    private void RestoreAbilities()
    {
        if (PlayerAbilitiesCache.HasData)
        {
            if (PlayerAbilitiesCache.EquippedAbility1 != null)
            {
                abilityHolder.EquipAbility(PlayerAbilitiesCache.EquippedAbility1, 1);
            }
            else
            {
                abilityHolder.UnequipAbility(1);
            }
            
            if (PlayerAbilitiesCache.EquippedAbility2 != null)
            {
                abilityHolder.EquipAbility(PlayerAbilitiesCache.EquippedAbility2, 2);
            }
            else
            {
                abilityHolder.UnequipAbility(2);
            }
        }
    }

    private void SaveAbilities()
    {
        PlayerAbilitiesCache.EquippedAbility1 = abilityHolder.GetEquippedAbility(1);
        PlayerAbilitiesCache.EquippedAbility2 = abilityHolder.GetEquippedAbility(2);
        PlayerAbilitiesCache.HasData = true;
    }
}
