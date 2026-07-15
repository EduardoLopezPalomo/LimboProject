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
}

public class OptionMenuNavigation : MonoBehaviour
{
    public GameObject LastMenu;
    public GameObject GameplayMenu;
    public List<NavigationButtons> navigationButtons;
    
    private GameObject currentMenu;

    void OnEnable()
    {
        ShowMenu(GameplayMenu);
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
        LastMenu.SetActive(true);
        gameObject.SetActive(false);
    }

    public void UpdateNavigation(Button lastClickButton)
    {
        Navigation nav = lastClickButton.navigation;
        nav.mode = Navigation.Mode.Explicit;
        nav.selectOnRight = null;
        
        foreach (var data in navigationButtons)
        {
            if (lastClickButton == data.ActualButton)
            {
                nav.selectOnRight = data.RightButtonNav;
                break;
            }
        }
        
        lastClickButton.navigation = nav;
    }
}
