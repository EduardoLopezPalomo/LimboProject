using System.Collections;
using UnityEngine;


public class TeleportSpec : GameplayAbilitySpec
{
    private CharacterVFX characterVFX;
    private SlotType slotType;
    public TeleportSpec(GameplayAbility ability, GameObject owner, SlotType slot) : base(ability, owner)
    {
        characterVFX = owner.GetComponent<CharacterVFX>();
        slotType = slot;
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
        Slot slot = SlotsManager.Instance.GetRandomSlot(owner, slotType);
        if (slot == null)
        {
            EndAbility();
            yield break;
        }
        
        owner.transform.position = slot.position;
        SlotsManager.Instance.ReleaseSlot(slot);

        EndAbility();
    }
    

}
