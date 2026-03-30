using UnityEngine;
using UnityEngine.UI;

public class AbilityButton : MonoBehaviour
{
    private GameplayAbility ability;
    private AbilityHolder holder;
    private static int slotCounter = 1; // Contador para asignar slots automáticamente

    public void Setup(GameplayAbility abl, AbilityHolder hld)
    {
        ability = abl;
        holder = hld;

        // Al hacer click normal (izquierdo), va al Slot 1
        // Si quieres algo más complejo (como un submenú), se puede añadir
        GetComponent<Button>().onClick.AddListener(EquipInSlot);
    }

    void EquipInSlot()
    {
        if(slotCounter == 1)
        {
            slotCounter = 2;
        }
        else
        {
            slotCounter = 1;
        }

        holder.EquipAbility(ability, slotCounter);
        Debug.Log(ability.name + " equipada en Slot" + slotCounter);
    }
}

