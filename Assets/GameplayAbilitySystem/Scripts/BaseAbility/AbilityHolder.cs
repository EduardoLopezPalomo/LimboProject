using System.Collections.Generic;
using UnityEngine;
/*
 *  This class is teh one that has all the abiliites of the character
 *  It has the list of abilities and a dictionary
 */
public class AbilityHolder : MonoBehaviour
{
    public GameplayAbilitySpec ActiveSpec;

    [SerializeField] private List<GameplayAbility> abilities;

    private Dictionary<string, GameplayAbilitySpec> specs;

    private void Awake()
    {

        specs = new Dictionary<string, GameplayAbilitySpec>();
        foreach (var ability in abilities)
        {
            if (ability != null && !specs.ContainsKey(ability.Name))
            {
                specs.Add(ability.Name, ability.CreateSpec(gameObject));
            }
        }
    }
    /*It add the ability to the dictionary*/
    public void AddAbility(GameplayAbility ability)
    {
        if (ability == null || specs.ContainsKey(ability.Name)) return;
        abilities.Add(ability);
        specs.Add(ability.Name, ability.CreateSpec(gameObject));
    }
    /*It removes the ability in the dictionary*/
    public void RemoveAbility(GameplayAbility ability)
    {
        if (ability == null) return;
        abilities.Remove(ability);
        specs.Remove(ability.Name);
    }
    /*Activates the ability*/
    public void ActivateAbility(string abilityName, GameObject owner)
    {
        if (specs.TryGetValue(abilityName, out var ability))
        {
            ActiveSpec = ability;
            ability.ActivateAbility();
        }
        else
        {
            Debug.LogWarning($"AbilityHolder: Ability {abilityName} not found");
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
