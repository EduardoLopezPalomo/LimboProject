using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class VibrationManager : MonoBehaviour
{
    public static VibrationManager Instance;
    
    public bool vibrationEnabled = true;
    [Range(0, 1)]
    public float intensity = 1f;
    
    private Coroutine vibrationCoroutine;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        RevertToSaved();
    }

    public void SetIntensity(float value)
    {
        intensity = value;
        Vibrate(0, 0.3f, 0.5f);
    }
    
    public void SetVibration(bool value)
    {
        vibrationEnabled = value;
    }

    public void RevertToSaved()
    {
        SettingsData data = SaveSystem.LoadSettingsData();
        if (data == null) return;
        
        GameplayData gameplayData = data.gameplayData;

        if (gameplayData != null)
        {
            vibrationEnabled = gameplayData.vibrationEnabled;
            intensity = gameplayData.vibrationIntensity;   
        }
    }
    
    public void Vibrate(float lowFreq, float highFreq, float duration)
    {
        if (Gamepad.current == null || !vibrationEnabled) return; 

        if (vibrationCoroutine != null)
            StopCoroutine(vibrationCoroutine);

        vibrationCoroutine = StartCoroutine(VibrateRoutine(lowFreq, highFreq, duration));
    }
    
    private IEnumerator VibrateRoutine(float lowFreq, float highFreq, float duration)
    {
        Gamepad.current.SetMotorSpeeds(lowFreq * intensity, highFreq * intensity);
        yield return new WaitForSecondsRealtime(duration);
        Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
    
    public void StopVibration()
    {
        if (vibrationCoroutine != null)
            StopCoroutine(vibrationCoroutine);
        Gamepad.current?.SetMotorSpeeds(0f, 0f);
    }

    void OnApplicationQuit()
    {
        Gamepad.current?.SetMotorSpeeds(0f, 0f);
    }
}
