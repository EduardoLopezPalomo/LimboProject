using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
/*
 *  This class is teh one that has all the abiliites of the character
 *  It has the list of abilities and a dictionary
 */
public class AbilityHolder : MonoBehaviour
{
    [SerializeField] private List<GameplayAbility> abilities;

    private Dictionary<int, GameplayAbilitySpec> specs;

    private GameplayAbility abilitySlot1;
    private GameplayAbility abilitySlot2;

    public List<GameplayAbilitySpec> ActiveSpecs;

    public event Action<int> OnUnequipAbility;
    public event Action<int> OnEquipAbility;
    public event Action<int, GameplayAbilitySpec> OnActivateEquipAbility;

    private void Awake()
    {
        ActiveSpecs = new List<GameplayAbilitySpec>();
        specs = new Dictionary<int, GameplayAbilitySpec>();
        int aux = 1;
        foreach (var ability in abilities)
        {
            if (ability != null && !specs.ContainsKey(ability.Id))
            {
                GameplayAbilitySpec spec = ability.CreateSpec(gameObject);
                spec.OnAbilityEnded += RemoveFromSpec;
                specs.Add(ability.Id, spec);
                ActivePasiveAbilities(spec);
                if (ability.equipable && aux <= 2)
                {
                    EquipAbility(ability, aux);
                    aux++;
                }
            }
        }
    }
    /*It add the ability to the dictionary*/
    public void AddAbility(GameplayAbility ability)
    {
        if (ability == null || specs.ContainsKey(ability.Id)) return;
        GameplayAbilitySpec spec = ability.CreateSpec(gameObject);
        spec.OnAbilityEnded += RemoveFromSpec;
        abilities.Add(ability);
        specs.Add(ability.Id, spec);

        ActivePasiveAbilities(spec);
    }
    /*It removes the ability in the dictionary*/
    public void RemoveAbility(GameplayAbility ability)
    {
        if (ability == null) return;
        if (specs.TryGetValue(ability.Id, out var spec))
        {
            spec.OnAbilityEnded -= RemoveFromSpec;
        }
        abilities.Remove(ability);
        specs.Remove(ability.Id);
    }
    /*Activates normal ability*/

    public GameplayAbilitySpec ActivateAbility(string abilityName, GameObject owner) => ActivateAbility(Animator.StringToHash(abilityName), owner);
    public GameplayAbilitySpec ActivateAbility(int abilityId, GameObject owner)
    {
        if (specs.TryGetValue(abilityId, out var ability))
        {
            bool alreadyActive = ActiveSpecs.Contains(ability);

            if ((!alreadyActive || ability.AllowReactivationWhileActive) && ability.CanActivateAbility())
            {
                if (!alreadyActive)
                    ActiveSpecs.Add(ability);

                ability.ActivateAbility();
                return ability;
            }
        }
        else
        {
            Debug.LogWarning($"AbilityHolder: Ability {abilityId} not found");
        }

        return null;
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

        GameplayAbilitySpec abilitySpec = ActivateAbility(equipped.Name, owner);
        OnActivateEquipAbility?.Invoke(slot, abilitySpec);
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
                if (abilitySlot2 == ability)
                {
                    UnequipAbility(2);
                }
                OnEquipAbility?.Invoke(slot);
                return true;
            case 2:
                abilitySlot2 = ability;
                if (abilitySlot1 == ability)
                {
                    UnequipAbility(1);
                }
                OnEquipAbility?.Invoke(slot);
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
                OnUnequipAbility?.Invoke(1);
                return true;
            case 2:
                abilitySlot2 = null;
                OnUnequipAbility?.Invoke(2);
                return true;
            default:
                Debug.LogWarning($"AbilityHolder: Invalid ability slot {slot}");
                return false;
        }
    }
    
    public bool IsAbilityActive(string abilityName) => IsAbilityActive(Animator.StringToHash(abilityName));

    public bool IsAbilityActive(int abilityId)
    {
        GameplayAbilitySpec ability = GetAbility(abilityId);
        if (ability == null) return false;

        return ActiveSpecs.Contains(ability);
    }
    
    public bool HasAnyActiveAbility(GameplayAbilitySpec excludeSpec)
    {
        return ActiveSpecs.Any(spec => spec != excludeSpec);
    }

    public bool HasAbility(string abilityName) => specs.ContainsKey(Animator.StringToHash(abilityName));

    public bool HasAbility(int abilityId) => specs.ContainsKey(abilityId);

    public GameplayAbilitySpec GetAbility(string abilityName)
        => specs.TryGetValue(Animator.StringToHash(abilityName), out var s) ? s : null;

    public GameplayAbilitySpec GetAbility(int abilityId)
        => specs.TryGetValue(abilityId, out var s) ? s : null;

    public List<GameplayAbility> GetAbilityList()
    {
        return abilities;
    }

    private void RemoveFromSpec(GameplayAbilitySpec spec)
    {
        ActiveSpecs.Remove(spec);
    }

    public void CancelAbility(string abilityName) => CancelAbility(Animator.StringToHash(abilityName));

    public void CancelAbility(int abilityId)
    {
        if (specs.TryGetValue(abilityId, out var ability))
        {
            ability.CancelAbility();
            RemoveFromSpec(ability);
        }
        else
        {
            Debug.LogWarning($"AbilityHolder: Ability id {abilityId} not found");
        }
    }

    public void CancelAllAbility()
    {
        foreach (var activeAbility in ActiveSpecs.ToList())
        {
            activeAbility.CancelAbility();
            RemoveFromSpec(activeAbility);
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
