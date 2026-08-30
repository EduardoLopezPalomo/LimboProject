using UnityEngine;

[System.Serializable]
public class GameplayData
{
    public bool vibrationEnabled;
    public float vibrationIntensity;

    public GameplayData()
    {
        vibrationEnabled = VibrationManager.Instance.vibrationEnabled;
        vibrationIntensity = VibrationManager.Instance.intensity;
    }
}
