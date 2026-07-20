using System;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[System.Serializable]
public class SelectedEvent : UnityEvent<AbilitySlotInventory>{ }

public class AbilitySlotInventory : MonoBehaviour, ISelectHandler
{
    public Button Button;
    public Image AbilityImage;
    
    [Header("Events")]
    public SelectedEvent OnSelected;
    
    private Sprite ButtonImageSprite;

    public void ChangeSprite(Sprite sprite)
    {
        AbilityImage.sprite = sprite;
        AbilityImage.color = Color.white;
    }

    public void DefaultSprite()
    {
        AbilityImage.sprite = null;
        AbilityImage.color = new Color32(255, 255, 255, 0);
    }
    
    public void OnSelect(BaseEventData eventData)
    {
        OnSelected?.Invoke(this);
    }
}
