using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class TrapBubbleSpec : GameplayAbilitySpec
{

    private float TrapTime;
    private GameObject bubble;

    private GameObject player;

    public TrapBubbleSpec(GameplayAbility ability, GameObject owner, float trapTime, GameObject bubbleObject) : base(ability, owner)
    {
        TrapTime = trapTime;
        player = owner;
        bubble = bubbleObject;

    }

    public override void ActivateAbility()
    {
        if (bOnCooldown)
        {
            return;
        }
        base.ActivateAbility();
        Vector3 initialPosition = player.transform.position + new Vector3(0f, 1f, 0f);
        initialPosition += player.transform.forward * 1f;
        Object obj = Object.Instantiate(bubble, initialPosition, player.transform.rotation);
        BubbleTrap trap = obj.GetComponent<BubbleTrap>();
        trap.InitializeBubble(TrapTime);
    }


    public override void EndAbility()
    {
        base.EndAbility();
    }
}
