using UnityEngine;
using System;

[CreateAssetMenu(menuName = "AbilitySystem/Responses/PlayerHealth")]
public class PlayerHealthResponse : GeneralHealthResponse
{
    public static event Action OnPlayerHurt;

    protected override void HurtBehavior(AttributeSystem owner, bool CanResponse)
    {
        
        CharacterVFX vfx = owner.GetComponent<CharacterVFX>();
        CharacterSFX sfx = owner.GetComponent<CharacterSFX>();
        if (vfx != null && sfx != null)
        {
            vfx.SpawnVFX(VFXType.Damaged);
            sfx.SpawnRandomSFX(SFXType.Damaged);
        }
        
        if (!CanResponse) return;

        KnockBack knockBack = owner.GetComponent<KnockBack>();
        if (knockBack)
        {
            knockBack.StartKnockBack();
        }
        OnPlayerHurt?.Invoke();
    }

    protected override void DeathBehavior(AttributeSystem owner)
    {
        KnockBack knockBack = owner.GetComponent<KnockBack>();

        knockBack.StartKnockBack();
    }
}
