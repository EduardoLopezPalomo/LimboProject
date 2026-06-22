using System.Collections.Generic;
using System;
using UnityEngine;
/*
 *  This class is teh one that has all the abiliites of the character
 *  It has the list of abilities and a dictionary
 */
public class AbilityHolder : MonoBehaviour
{
    [SerializeField] private List<GameplayAbility> abilities;

    private Dictionary<string, GameplayAbilitySpec> specs;

    private GameplayAbility abilitySlot1;
    private GameplayAbility abilitySlot2;

    public List<GameplayAbilitySpec> ActiveSpecs;

    public event Action OnLoadoutChanged;

    public IReadOnlyList<GameplayAbility> OwnedAbilities => abilities;

    private void Awake()
    {
        ActiveSpecs = new List<GameplayAbilitySpec>();
        specs = new Dictionary<string, GameplayAbilitySpec>();
        foreach (var ability in abilities)
        {
            if (ability != null && !specs.ContainsKey(ability.Name))
            {
                GameplayAbilitySpec spec = ability.CreateSpec(gameObject);
                specs.Add(ability.Name, spec);
                ActivePasiveAbilities(spec);
            }
        }
    }
    /*It add the ability to the dictionary*/
    public void AddAbility(GameplayAbility ability)
    {
        if (ability == null || specs.ContainsKey(ability.Name)) return;
        GameplayAbilitySpec spec = ability.CreateSpec(gameObject);

        abilities.Add(ability);
        specs.Add(ability.Name, spec);

        ActivePasiveAbilities(spec);
    }
    /*It removes the ability in the dictionary*/
    public void RemoveAbility(GameplayAbility ability)
    {
        if (ability == null) return;
        abilities.Remove(ability);
        specs.Remove(ability.Name);
    }
    /*Activates normal ability*/
    public void ActivateAbility(string abilityName, GameObject owner)
    {
        if (specs.TryGetValue(abilityName, out var ability))
        {
            if (!ActiveSpecs.Contains(ability))
            {
                ActiveSpecs.Add(ability);   
            }
            ability.ActivateAbility();
        }
        else
        {
            Debug.LogWarning($"AbilityHolder: Ability {abilityName} not found");
        }
    }
    /*Activates equipped ability*/
    public void ActivateEquippedAbility(int slot, GameObject owner)
    {
        if (slot < 0 || slot >= 3)
        {
            Debug.LogWarning($"AbilityHolder: Invalid ability slot {slot}");
            return;
        }
        GameplayAbility equipped = GetEquippedAbility(slot);
        if (equipped == null)
        {
            Debug.LogWarning($"AbilityHolder: No ability equipped in slot {slot}");
            return;
        }
        else
        {
            ActivateAbility(equipped.Name, owner);
        }
    }

    /*Returns the equipped ability in that slot*/
    public GameplayAbility GetEquippedAbility(int slot)
    {
        switch (slot)
        {
            case 1:
                return abilitySlot1;
            case 2:
                return abilitySlot2;
            default:
                Debug.LogWarning($"AbilityHolder: Invalid ability slot {slot}");
                return null;
        }
    }

    /*Equips an ability in that slot*/
    public bool EquipAbility(GameplayAbility ability, int slot)
    {
        if (ability == null || slot < 1 || slot > 2)
        {
            Debug.LogWarning($"AbilityHolder: Invalid ability or slot {slot}");
            return false;
        }
        switch (slot)
        {
            case 1:
                abilitySlot1 = ability;
                OnLoadoutChanged?.Invoke();
                return true;
            case 2:
                abilitySlot2 = ability;
                OnLoadoutChanged?.Invoke();
                return true;
            default:
                Debug.LogWarning($"AbilityHolder: Invalid ability slot {slot}");
                return false;
        }
    }
    /*Unequips an ability in that slot*/
    public bool UnequipAbility(int slot)
    {
        if (slot < 1 || slot > 2)
        {
            Debug.LogWarning($"AbilityHolder: Invalid ability slot {slot}");
            return false;
        }
        switch (slot)
        {
            case 1:
                abilitySlot1 = null;
                OnLoadoutChanged?.Invoke();
                return true;
            case 2:
                abilitySlot2 = null;
                OnLoadoutChanged?.Invoke();
                return true;
            default:
                Debug.LogWarning($"AbilityHolder: Invalid ability slot {slot}");
                return false;
        }
    }

    public bool HasAbility(string abilityName)
    {
        if (specs.TryGetValue(abilityName, out var ability))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void CancelAbility(string abilityName)
    {
        if (specs.TryGetValue(abilityName, out var ability))
        {
            ability.CancelAbility();
            ActiveSpecs.Remove(ability);
        }
        else
        {
            Debug.LogWarning($"AbilityHolder: Ability {abilityName} not found");
        }
    }

    private void ActivePasiveAbilities(GameplayAbilitySpec spec)
    {
        if (spec.ability.isPasive)
        {
            ActiveSpecs.Add(spec);
        }
    }

    /*Does the tick for every spec if they doesnt have cooldown they dont use it*/
    void Update()
    {
        float dt = Time.deltaTime;

        foreach (var spec in specs.Values)
        {
            spec.Tick(dt);
        }

    }
}
