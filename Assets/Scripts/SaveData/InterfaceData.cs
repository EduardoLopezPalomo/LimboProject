using UnityEngine;

[System.Serializable]
public class InterfaceData
{
    public bool showHUD;
    public float sizeHUD;
    public float opacityHUD;
    public bool showTutorial;

    public InterfaceData() { }

    public InterfaceData(InterfaceManager interfaceManager)
    {
        showHUD = interfaceManager.showHUD;
        sizeHUD = interfaceManager.sizeHUD;
        opacityHUD = interfaceManager.opacityHUD;
        showTutorial = interfaceManager.showTutorial;   
    }
}
