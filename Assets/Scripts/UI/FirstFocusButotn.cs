using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FirstFocusButotn : MonoBehaviour
{
    public Button firstButton;

    void OnEnable()
    {
        StartCoroutine(SelectButtonNextFrame());
    }
    
    private void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            SelectButton();
        }
    }
    
    private IEnumerator SelectButtonNextFrame()
    {
        yield return new WaitForEndOfFrame(); 
        
        SelectButton();
    }

    private void SelectButton()
    {
        EventSystem.current.SetSelectedGameObject(null); 
        ButtonSoundManager.IgnoreNextSelection = true;
        EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
    }
   
    
}
