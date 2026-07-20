using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeColorButton : MonoBehaviour
{
    public Color Preseedcolor = Color.white;
    public Color Unpreseedcolor = Color.white;
    public List<Button> buttons;

    public void ChangeColor(Button preesedB)
    {
        foreach (var button in buttons)
        {
            if (button == preesedB)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = Preseedcolor;
                button.colors = colors;
            }
            else
            {
                ColorBlock colors = button.colors;
                colors.normalColor = Unpreseedcolor;
                button.colors = colors;
            }
        }
        
    }
}
