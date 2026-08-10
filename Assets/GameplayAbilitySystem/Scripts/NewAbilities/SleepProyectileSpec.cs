using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class SleepProyectileSpec : GameplayAbilitySpec
{

    private GameObject proyectile;

    private GameObject player;

    public SleepProyectileSpec(GameplayAbility ability, GameObject owner, GameObject proyectileObject) : base(ability, owner)
    {
        player = owner;
        proyectile = proyectileObject;

    }

    public override void ActivateAbility()
    {
        if (bOnCooldown)
        {
            EndAbility();
            return;
        }
        base.ActivateAbility();
        Vector3 initialPosition = player.transform.position + new Vector3(0f, 1f, 0f);
        initialPosition += player.transform.forward * 1f;
        Object obj = Object.Instantiate(proyectile, initialPosition, player.transform.rotation);
        EndAbility();
    }

}
