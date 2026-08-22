using UnityEngine;
using System;

[CreateAssetMenu(menuName = "AbilitySystem/Responses/EnemyHealth")]
public class EnemyHealthResponse : GeneralHealthResponse
{
    public static event Action OnEnemyHurt;
    
    protected override void HurtBehavior(AttributeSystem owner, bool CanResponse)
    {
        if (CanResponse)
        {
            var knockBack = owner.GetComponent<KnockBack>();
            var enemy = owner.GetComponent<EnemyGraphBridge>();
            if (enemy != null)
            {
                enemy.SetHurt();
            }

            if (knockBack != null)
            {
                knockBack.StartKnockBack();
            }
        }
        CharacterVFX vfx = owner.GetComponent<CharacterVFX>();
        CharacterSFX sfx = owner.GetComponent<CharacterSFX>();
        if (vfx != null && sfx != null)
        {
            vfx.SpawnVFX(VFXType.Damaged);
            sfx.SpawnRandomSFX(SFXType.Damaged);
        }
        
        EnemyUIbridge UI = owner.GetComponent<EnemyUIbridge>();
        if (UI != null)
        {
            UI.ShowHealth();
        }
        OnEnemyHurt?.Invoke();
    }

    protected override void DeathBehavior(AttributeSystem owner)
    {
        DeathCheck deathCheck = owner.gameObject.GetComponent<DeathCheck>();
        if (deathCheck)
        {
            if (deathCheck.GetIsDeath()) return;

            deathCheck.SetIsDeath(true);   
        }
        GameObject target = owner.gameObject;
        EnemyGraphBridge enemy = owner.GetComponent<EnemyGraphBridge>();
        CharacterSFX sfx = owner.GetComponent<CharacterSFX>();
        if (sfx != null)
        {
            sfx.SpawnRandomSFX(SFXType.Death);
        }

        enemy.SetDeath();

        KarmaManager.Instance.AddKarma(-10);

        Destroy(target, 2f);
    }

    protected override void OverHealthBehavior(AttributeSystem owner)
    {
        EnemyUIbridge UI = owner.GetComponent<EnemyUIbridge>();
        if (UI != null)
        {
            UI.ShowHealth();
        }
    }
}
