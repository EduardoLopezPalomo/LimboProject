using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public class DamageProyectileSpec : GameplayAbilitySpec
{
    private float angle;

    private float lateralOffset;
    private GameObject proyectile;
    private GameObject player;

    public DamageProyectileSpec(GameplayAbility ability, GameObject owner, GameObject proyectileObject, float throwAngle, float lateralChange) : base(ability, owner)
    {
        player = owner;
        proyectile = proyectileObject;
        angle = throwAngle;
        lateralOffset = lateralChange;

    }

    public override void ActivateAbility()
    {
        if (bOnCooldown)
        {
            return;
        }
        base.ActivateAbility();
        Vector3 initialPosition = player.transform.position + new Vector3(0f, 1f, 0f);
        initialPosition += player.transform.forward * 0.2f;

        Vector3 right = player.transform.right;

        Vector3 position1 = initialPosition + right * lateralOffset;
        Vector3 position2 = initialPosition - right * lateralOffset;

        Vector3 direction1 = Quaternion.Euler(0f, angle, 0f) * player.transform.forward;
        Vector3 direction2 = Quaternion.Euler(0f, -angle, 0f) * player.transform.forward;

        CreateProyectile(initialPosition, player.transform.rotation);
        CreateProyectile(position1, Quaternion.LookRotation(direction1));
        CreateProyectile(position2, Quaternion.LookRotation(direction2));
    }

    private void CreateProyectile(Vector3 position, Quaternion direction)
    {
        Object.Instantiate(proyectile, position, direction);
    }


    public override void EndAbility()
    {
        base.EndAbility();
    }
}
