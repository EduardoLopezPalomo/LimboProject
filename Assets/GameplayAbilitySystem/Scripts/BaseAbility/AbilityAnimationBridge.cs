using UnityEngine;

public class AbilityAnimationBridge : MonoBehaviour
{
    public AbilityHolder abilityHolder;

    private GameplayAbilitySpec currentSpec => abilityHolder.ActiveSpec;

    /*
     *  Notifies of LightAttack
     */
    public void ComboWindowOpen()
    {
        (currentSpec as LightAttackSpec)?.ComboWindowOpen();
    }

    public void ComboWindowClose()
    {
        (currentSpec as LightAttackSpec)?.ComboWindowClose();
    }

    public void EnableRootMotion()
    {
        (currentSpec as LightAttackSpec)?.EnableRootMotion();
    }

    public void DisableRootMotion()
    {
        (currentSpec as LightAttackSpec)?.DisableRootMotion();
    }

    public void EnableSwordCollider()
    {
        (currentSpec as LightAttackSpec)?.EnableSwordCollider();
    }

    public void DisableSwordCollider()
    {
        (currentSpec as LightAttackSpec)?.DisableSwordCollider();
    }
}
