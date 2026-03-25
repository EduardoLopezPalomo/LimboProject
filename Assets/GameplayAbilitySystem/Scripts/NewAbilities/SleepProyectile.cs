using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Ability/SleepProyectile")]
public class SleepProyectile : GameplayAbility
{
    public GameObject proyectile;
    public override GameplayAbilitySpec CreateSpec(GameObject owner)
    {
        return new SleepProyectileSpec(this, owner, proyectile);
    }
}
