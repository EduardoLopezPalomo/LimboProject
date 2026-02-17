using System.Collections.Generic;
using UnityEngine;

public class PlayerAbilityController : MonoBehaviour
{
    [SerializeField] private AbilityHolder abilityHolder;

    public void ActivateAbility(string abilityName)
    {
        if (abilityHolder == null) return;
        abilityHolder.ActivateAbility(abilityName, gameObject);
    }
}
