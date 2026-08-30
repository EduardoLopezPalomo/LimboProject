using UnityEngine;

[System.Serializable]
public class GameplayData
{
    public bool vibrationEnabled;
    public float vibrationIntensity;

    public GameplayData() { }
    
    public GameplayData(VibrationManager vibrationManager)
    {
        vibrationEnabled = vibrationManager.vibrationEnabled;
        vibrationIntensity = vibrationManager.intensity;
    }
}
