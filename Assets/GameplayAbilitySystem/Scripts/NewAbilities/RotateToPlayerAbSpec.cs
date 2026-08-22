using System.Collections;
using UnityEngine;


public class RotateToPlayerAbSpec : GameplayAbilitySpec
{
    private RotateToPlayer rotateToPlayer;
    public RotateToPlayerAbSpec(GameplayAbility ability, GameObject owner) : base(ability, owner)
    {
        rotateToPlayer = owner.GetComponent<RotateToPlayer>();
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();

        rotateToPlayer.RotateInstant();
        EndAbility();
    }
    

}
