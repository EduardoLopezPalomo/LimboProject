using UnityEngine;

public class ControlPanelSwitcher : MonoBehaviour
{
    public GameObject controllerPanel;
    public GameObject keyboardPanel;

    public void ShowControllerPanel()
    {
        controllerPanel.SetActive(true);
        keyboardPanel.SetActive(false);
    }

    public void ShowKeyboardPanel()
    {
        keyboardPanel.SetActive(true);
        controllerPanel.SetActive(false);
    }
}

