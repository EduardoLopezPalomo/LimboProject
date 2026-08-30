using System;
using UnityEngine;

public class InterfaceManager : MonoBehaviour
{
    public static InterfaceManager Instance;
    public static event Action OnManagerInstance;

    [HideInInspector] public bool showHUD = true;
    [Range(0, 1)]
    [HideInInspector] public float sizeHUD = 1f;
    [Range(0, 1)]
    [HideInInspector] public float opacityHUD = 1f;
    [HideInInspector] public bool showTutorial = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else 
            Destroy(this);
        
        RevertToSave();
        OnManagerInstance?.Invoke();
    }

    public void RevertToSave()
    {
        SettingsData data = SaveSystem.LoadSettingsData();

        if (data == null) return;
        
        InterfaceData interfaceData = data.interfaceData;
        
        if(interfaceData == null) return;
        
        showHUD = interfaceData.showHUD;
        sizeHUD = interfaceData.sizeHUD;
        opacityHUD = interfaceData.opacityHUD;
        showTutorial = interfaceData.showTutorial;
    }

}
