using UnityEngine;
using UnityEngine.UI;

public class InterfaceMenu : MonoBehaviour
{
    public Toggle showHUD;
    public Slider sizeHUD;
    public Slider opacityHUD;
    public Toggle showTutorial;
    
    private void OnEnable()
    {
        showHUD.isOn = InterfaceManager.Instance.showHUD;
        sizeHUD.value = InterfaceManager.Instance.sizeHUD;
        opacityHUD.value = InterfaceManager.Instance.opacityHUD;
        showTutorial.isOn = InterfaceManager.Instance.showTutorial;
    }

    public void OnSizeHUDChange(float value)
    {
        InterfaceManager.Instance.sizeHUD = value;
    }
    
    public void OnOpacityHUDChange(float value)
    {
        InterfaceManager.Instance.opacityHUD = value;
    }

    public void OnShowHUDChange(bool value)
    {
        InterfaceManager.Instance.showHUD = value;
    }
    
    public void OnShowTutorialChange(bool value)
    {
        InterfaceManager.Instance.showTutorial = value;
    }
    
    
    
}
