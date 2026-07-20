using UnityEngine.UI;
using UnityEngine;

public class AbilitySlotInventory : MonoBehaviour
{
    public Button Button;
    public Image Image;

    public void ChangeSprite(Sprite sprite)
    {
        Image.sprite = sprite;
        Image.color = Color.white;
    }
}
