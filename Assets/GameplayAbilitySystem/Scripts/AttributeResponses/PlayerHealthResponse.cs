using UnityEngine;

[CreateAssetMenu(menuName = "AbilitySystem/Responses/PlayerHealth")]
public class PlayerHealthResponse : GeneralHealthResponse
{
    protected override void HurtBehavior(AttributeSystem owner, bool CanResponse)
    {
        if (!CanResponse) return;
        
        KnockBack knockBack = owner.GetComponent<KnockBack>();
        if (knockBack)
        {
            knockBack.StartKnockBack();   
        }
    }

    protected override void DeathBehavior(AttributeSystem owner)
    {
        KnockBack knockBack = owner.GetComponent<KnockBack>();
                
        knockBack.StartKnockBack();
    }
}
