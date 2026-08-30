using System;
using UnityEngine;
using UnityEngine.UI;

public class GameplayMenu : MonoBehaviour
{
    public Toggle vibrationEnable;
    public Slider vibrationSlider;

    private void OnEnable()
    {
        vibrationEnable.isOn = VibrationManager.Instance.vibrationEnabled;
        vibrationSlider.value = VibrationManager.Instance.intensity;
    }

    public void OnSliderChange(float value)
    {
        VibrationManager.Instance.SetIntensity(value);
    }

    public void OnToggleChange(bool value)
    {
        VibrationManager.Instance.SetVibration(value);
    }
}
