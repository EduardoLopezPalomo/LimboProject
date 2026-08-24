using UnityEngine;
using System;

[CreateAssetMenu(menuName = "AbilitySystem/Responses/BossHealth")]
public class BossHealthResponse : EnemyHealthResponse
{

    protected override void DeathBehavior(AttributeSystem owner)
    {
        DeathCheck deathCheck = owner.gameObject.GetComponent<DeathCheck>();
        if (deathCheck)
        {
            if (deathCheck.GetIsDeath()) return;

            deathCheck.SetIsDeath(true);   
        }
        
        EnemyGraphBridge enemy = owner.GetComponent<EnemyGraphBridge>();
        CharacterSFX sfx = owner.GetComponent<CharacterSFX>();
        if (sfx != null)
        {
            sfx.SpawnRandomSFX(SFXType.Death);
        }

        enemy.SetDeath();

        KarmaManager.Instance.AddKarma(-10);
    }
    
}
