using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class SleepProjectileSpec : GameplayAbilitySpec
{

    private GameObject projectile;

    private GameObject player;

    public SleepProjectileSpec(GameplayAbility ability, GameObject owner, GameObject projectileObject) : base(ability, owner)
    {
        player = owner;
        projectile = projectileObject;

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
        Object obj = Object.Instantiate(projectile, initialPosition, player.transform.rotation);
        EndAbility();
    }

}
