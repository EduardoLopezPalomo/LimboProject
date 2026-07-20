using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[Serializable]
public struct NavigationButtons
{
    public Button ActualButton;
    public Selectable RightButtonNav;
    public Selectable DownButtonNav;
}

public class MenuNavigation : MonoBehaviour
{
    public GameObject LastMenu;
    public GameObject FirstMenu;
    public List<NavigationButtons> navigationButtons;
    
    private GameObject currentMenu;

    void OnEnable()
    {
        ShowMenu(FirstMenu);
    }
    
    public void ShowMenu(GameObject go)
    {
        if (currentMenu != null)
        {
            currentMenu.SetActive(false);
        }
        currentMenu = go;
        go.SetActive(true);
    }

    public void GoBack()
    {
        if (LastMenu != null)
        {
            LastMenu.SetActive(true);   
        }
        gameObject.SetActive(false);
        SaveSystem.SaveAudioData(SoundMixerManager.instance);
    }

    public void UpdateNavigation(Button lastClickButton)
    {
        Navigation nav = lastClickButton.navigation;
        nav.mode = Navigation.Mode.Explicit;
        nav.selectOnRight = null;
        nav.selectOnDown = null;
        
        foreach (var data in navigationButtons)
        {
            if (lastClickButton == data.ActualButton)
            {
                nav.selectOnRight = data.RightButtonNav;
                nav.selectOnDown = data.DownButtonNav;
                break;
            }
        }
        
        lastClickButton.navigation = nav;
    }
}
