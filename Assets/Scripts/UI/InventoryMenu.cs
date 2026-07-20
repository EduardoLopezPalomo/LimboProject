using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryMenu : MonoBehaviour
{
    public Button EquipAbilityButton;
    [SerializeField] private AbilityHolder playerAbilityHolder;
    [SerializeField] private List<AbilitySlotInventory> Slots;
    
    private Dictionary<AbilitySlotInventory, GameplayAbility> dictionary = new Dictionary<AbilitySlotInventory, GameplayAbility>();
    private Button LastButton;
    private AbilitySlotInventory WantToEquipSlot;

    void OnEnable()
    {
        List<GameplayAbility> abilities = playerAbilityHolder.GetAbilityList();
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i] == null)
            {
                Debug.LogWarning("la imagen " + i + " es null");
                continue;
            }
            if (i >= abilities.Count || abilities[i] == null)
            {
                Slots[i].Image.gameObject.SetActive(false);
                continue;
            }

            if (dictionary.ContainsKey(Slots[i])) continue;
            
            if(abilities[i].isPasive) continue;
            
            dictionary.Add(Slots[i], abilities[i]);
            Slots[i].ChangeSprite(abilities[i].icon);
        }
        
    }

    public void WantToEquipAbility(AbilitySlotInventory Slot)
    {
        if(!dictionary.ContainsKey(Slot) || !dictionary[Slot].equipable) return;
        
        SelectButton(EquipAbilityButton);
        LastButton = Slot.Button;
        WantToEquipSlot = Slot;
    }

    public void EquipAbility(AbilitySlotInventory Slot)
    {
        if (LastButton == null || WantToEquipSlot == null)
        {
            Debug.LogWarning("no se han inicializado bien el boton para volver o el slot");
        }
        
        GameplayAbility ability = dictionary[WantToEquipSlot];
        
        if (Slot.gameObject.name == "Slot1")
        {
            playerAbilityHolder.EquipAbility(ability, 1);
        }
        else if (Slot.gameObject.name == "Slot2")
        {
            playerAbilityHolder.EquipAbility(ability, 2);
        }
        else
        {
            Debug.LogWarning("El boton " + Slot.name + " no coincide con el nombre Slot1 o Slot2 ");
        }
        
        Slot.ChangeSprite(ability.icon);
        SelectButton(LastButton);
        LastButton = null;
        WantToEquipSlot = null;
    }

    private void SelectButton(Button button)
    {
        EventSystem.current.SetSelectedGameObject(null); 
        ButtonSoundManager.IgnoreNextSelection = true;
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}
