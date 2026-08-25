using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryMenu : MonoBehaviour
{
    public Button EquipAbilityButton;
    public TextMeshProUGUI TittleText;
    public TextMeshProUGUI DescriptionText;
    [SerializeField] private AbilityHolder playerAbilityHolder;
    [SerializeField] private List<AbilitySlotInventory> Slots;
    [SerializeField] private List<AbilitySlotInventory> PasiveSlots;
    [SerializeField] private GameObject SlotAbiity1;
    [SerializeField] private GameObject SlotAbiity2;
    
    private Dictionary<AbilitySlotInventory, GameplayAbility> dictionary = new Dictionary<AbilitySlotInventory, GameplayAbility>();
    private Button LastButton;
    private AbilitySlotInventory WantToEquipSlot;

    void OnEnable()
    {
        playerAbilityHolder.OnUnequipAbility += UnequipAbility;
        playerAbilityHolder.OnEquipAbility += RefreshEquippedSlot;
        
        List<GameplayAbility> abilities = playerAbilityHolder.GetAbilityList();
        int activeIndex = 0;
        int passiveIndex = 0;
        
        foreach (var slot in Slots)
            slot.AbilityImage.gameObject.SetActive(false);

        foreach (var slot in PasiveSlots)
            slot.AbilityImage.gameObject.SetActive(false);

        foreach (GameplayAbility ability in abilities)
        {
            if (ability == null)
            {
                Debug.LogWarning("Ability " + ability.name + " is null");
                continue;
            }

            if (ability.isPasive)
            {
                if (passiveIndex >= PasiveSlots.Count)
                    continue;
                
                var slot = PasiveSlots[passiveIndex++];
                slot.AbilityImage.gameObject.SetActive(true);
                dictionary[slot] = ability;
                slot.ChangeSprite(ability.icon);
            }
            else
            {
                if (activeIndex >= Slots.Count)
                    continue;

                var slot = Slots[activeIndex++];
                slot.AbilityImage.gameObject.SetActive(true);
                dictionary[slot] = ability;
                slot.ChangeSprite(ability.icon);
            }
        }
        
        RefreshEquippedSlot(1);
        RefreshEquippedSlot(2);
    }

    void OnDisable()
    {
        playerAbilityHolder.OnUnequipAbility -= UnequipAbility;
        playerAbilityHolder.OnEquipAbility -= RefreshEquippedSlot;
    }

    public void WantToEquipAbility(AbilitySlotInventory Slot)
    {
        if(!dictionary.ContainsKey(Slot) || !dictionary[Slot].equipable) return;
        
        SelectButton(EquipAbilityButton);
        LastButton = Slot.Button;
        WantToEquipSlot = Slot;
    }

    public void EquipAbility(GameObject SlotAbility)
    {
        if (LastButton == null || WantToEquipSlot == null)
        {
            Debug.LogWarning("no se han inicializado bien el boton para volver o el slot");
        }
        
        GameplayAbility ability = dictionary[WantToEquipSlot];
        
        if (SlotAbility == SlotAbiity1)
        {
            playerAbilityHolder.EquipAbility(ability, 1);
        }
        else if (SlotAbility == SlotAbiity2)
        {
            playerAbilityHolder.EquipAbility(ability, 2);
        }
        else
        {
            Debug.LogWarning("El boton " + SlotAbility.name + " no coincide con el  Slot1 o Slot2 ");
        }

        AbilitySlotInventory inv =  SlotAbility.GetComponent<AbilitySlotInventory>();
        if (inv != null)
        {
            inv.ChangeSprite(ability.icon);     
        }
        SelectButton(LastButton);
        LastButton = null;
        WantToEquipSlot = null;
    }

    private void UnequipAbility(int slot)
    {
        if (slot == 1)
        {
            AbilitySlotInventory inv =  SlotAbiity1.GetComponent<AbilitySlotInventory>();
            if (inv != null)
            {
                inv.DefaultSprite();     
            }
        }
        else if (slot == 2)
        {
            AbilitySlotInventory inv =  SlotAbiity2.GetComponent<AbilitySlotInventory>();
            if (inv != null)
            {
                inv.DefaultSprite();    
            }
        }
        else
        {
            Debug.LogWarning("Slot no es 1 o 2");
        }
    }

    public void ChangeDescription(AbilitySlotInventory Slot)
    {
        if (!dictionary.ContainsKey(Slot))
        {
            TittleText.text = "Locked";
            DescriptionText.text = "";
            return;
        }
            
        TittleText.text = dictionary[Slot].Name;
        DescriptionText.text = dictionary[Slot].Description;
    }

    private void SelectButton(Button button)
    {
        EventSystem.current.SetSelectedGameObject(null); 
        ButtonSoundManager.IgnoreNextSelection = true;
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
    
    private void RefreshEquippedSlot(int slot)
    {
        GameplayAbility ability = playerAbilityHolder.GetEquippedAbility(slot);
        GameObject slotObj = slot == 1 ? SlotAbiity1 : slot == 2 ? SlotAbiity2 : null;

        if (slotObj == null)
        {
            Debug.LogWarning("Slot no es 1 o 2");
            return;
        }

        AbilitySlotInventory inv = slotObj.GetComponent<AbilitySlotInventory>();
        if (inv == null) return;

        if (ability != null)
            inv.ChangeSprite(ability.icon);
        else
            inv.DefaultSprite();
    }
}
