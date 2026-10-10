using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PlayerAttributeCache holds captured attribute values for persistence.
/// Used to save player's current attributes before scene transitions.
/// </summary>
public static class PlayerAttributeCache
{
    /// <summary>
    /// Dictionary of attribute values captured for persistence.
    /// Key: attribute name, Value: float value
    /// </summary>
    public static Dictionary<string, float> Values;
    
    /// <summary>
    /// Flag indicating if attribute data has been saved.
    /// </summary>
    public static bool HasData;

    /// <summary>
    /// Clears all cached attribute data.
    /// Called before saving to the main save system.
    /// </summary>
    public static void Clear()
    {
        Values = null;
        HasData = false;
    }
}

/// <summary>
/// PlayerAbilitiesCache holds captured ability data for persistence.
/// Tracks equipped abilities and available abilities for restoration.
/// </summary>
public static class PlayerAbilitiesCache
{
    /// <summary>
    /// First equipped ability (slot 1).
    /// </summary>
    public static GameplayAbility EquippedAbility1;
    
    /// <summary>
    /// Second equipped ability (slot 2).
    /// </summary>
    public static GameplayAbility EquippedAbility2;
    
    /// <summary>
    /// List of all abilities currently available to the player.
    /// Used to restore ability pool on load.
    /// </summary>
    public static List<GameplayAbility> AllAbilities;
    
    /// <summary>
    /// Flag indicating if ability data has been saved.
    /// </summary>
    public static bool HasData;

    /// <summary>
    /// Clears all cached ability data.
    /// Called before saving to the main save system.
    /// </summary>
    public static void Clear()
    {
        EquippedAbility1 = null;
        EquippedAbility2 = null;
        AllAbilities = null;
        HasData = false;
    }
}

/// <summary>
/// PlayerGASPersistance handles persistence of the Gameplay Ability System (GAS) and attributes.
/// This component saves and restores:
/// - Player attributes (strength, agility, etc.)
/// - Equipped abilities
/// - Available abilities pool
/// 
/// Used when saving/loading player progress (scene changes, quit game, etc.)
/// </summary>
public class PlayerGASPersistance : MonoBehaviour
{
    private AttributeSystem attributeSystem;
    private AbilityHolder abilityHolder;

    private void Awake()
    {
        // Get references to the attribute and ability systems
        attributeSystem = GetComponent<AttributeSystem>();
        abilityHolder = GetComponent<AbilityHolder>();
    }

    /// <summary>
    /// Saves the current player state to caches.
    /// Called before scene transitions or save operations.
    /// </summary>
    public void SaveState()
    {
        SaveAbilities();
        SaveAttributes();
    }

    /// <summary>
    /// Restores the player state from caches.
    /// Called after loading save data or scene transitions.
    /// </summary>
    public void RestoreState()
    {
        RestoreAbilities();
        RestoreAttributes();
    }

    /// <summary>
    /// Restores player attributes from the cache.
    /// Applies values to the attribute system without notifying observers.
    /// </summary>
    private void RestoreAttributes()
    {
        if (PlayerAttributeCache.HasData)
        {
            attributeSystem.ApplyValues(PlayerAttributeCache.Values, false);
        }
    }

    /// <summary>
    /// Captures current attribute values from the attribute system.
    /// Stores them in the cache for later restoration.
    /// </summary>
    private void SaveAttributes()
    {
        PlayerAttributeCache.Values = attributeSystem.CaptureValues();
        PlayerAttributeCache.HasData = true;
    }

    /// <summary>
    /// Restores player abilities from the cache.
    /// 1. Restores ability pool (adds abilities if not already owned)
    /// 2. Restores equipped abilities to their slots
    /// </summary>
    private void RestoreAbilities()
    {
        if (PlayerAbilitiesCache.HasData)
        {
            // Restore ability pool
            foreach (var ability in PlayerAbilitiesCache.AllAbilities)
            {
                if (!abilityHolder.HasAbility(ability.Id))
                {
                    abilityHolder.AddAbility(ability);
                }
            }

            // Restore first equipped ability
            if (PlayerAbilitiesCache.EquippedAbility1 != null)
            {
                abilityHolder.EquipAbility(PlayerAbilitiesCache.EquippedAbility1, 1);
            }
            else
            {
                abilityHolder.UnequipAbility(1);
            }

            // Restore second equipped ability
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

    /// <summary>
    /// Saves current ability state to the cache.
    /// Captures:
    /// - Equipped abilities in slots 1 and 2
    /// - Full list of available abilities
    /// </summary>
    private void SaveAbilities()
    {
        PlayerAbilitiesCache.EquippedAbility1 = abilityHolder.GetEquippedAbility(1);
        PlayerAbilitiesCache.EquippedAbility2 = abilityHolder.GetEquippedAbility(2);
        PlayerAbilitiesCache.HasData = true;
        PlayerAbilitiesCache.AllAbilities = abilityHolder.GetAbilityList();
    }
}
