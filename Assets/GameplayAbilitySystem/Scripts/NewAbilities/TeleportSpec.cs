using System.Collections;
using UnityEngine;


public class TeleportSpec : GameplayAbilitySpec
{
    private CharacterVFX characterVFX;
    public TeleportSpec(GameplayAbility ability, GameObject owner) : base(ability, owner)
    {
        characterVFX = owner.GetComponent<CharacterVFX>();
    }

    public override void ActivateAbility()
    {
        base.ActivateAbility();
        
        characterVFX.SpawnVFX(VFXType.Teleport);
        characterVFX.StartCoroutine(TeleportAfterDelay(0.1f));
    }

    private IEnumerator TeleportAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Slot slot = SlotsManager.Instance.GetRandomSlot(owner, SlotType.Far);
        if (slot == null)
        {
            EndAbility();
            yield break;
        }
        
        owner.transform.position = slot.position;

        EndAbility();
    }
    

}
