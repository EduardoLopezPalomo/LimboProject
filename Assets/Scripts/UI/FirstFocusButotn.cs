using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FirstFocusButotn : MonoBehaviour
{
    public Button firstButton;

    void OnEnable()
    {
        EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
    }
}
