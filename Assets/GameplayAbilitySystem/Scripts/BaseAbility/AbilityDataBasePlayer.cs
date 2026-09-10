using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "AbilityDatabase", menuName = "Gameplay/Ability Database")]
public class AbilityDataBasePlayer : ScriptableObject
{
    [SerializeField] private List<GameplayAbility> allAbilities;

    private Dictionary<int, GameplayAbility> _lookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<int, GameplayAbility>();
        foreach (var ability in allAbilities)
        {
            if (ability == null) continue;

            if (!_lookup.TryAdd(ability.Id, ability))
            {
                Debug.LogWarning($"Id duplicado para {ability.Name} (colisión de hash). Revisa los nombres.");
            }
        }
    }

    public GameplayAbility GetById(int id)
    {
        if (_lookup == null) BuildLookup();

        _lookup.TryGetValue(id, out var ability);
        return ability;
    }
}
