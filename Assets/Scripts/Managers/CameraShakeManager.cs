using System;
using Cinemachine;
using UnityEngine;

public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager instance;
    
    [SerializeField] private CinemachineImpulseListener impulseListener;
    
    private CinemachineImpulseDefinition impulseDefinition;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void SetImpulseListener(CinemachineImpulseListener listener)
    {
        impulseListener = listener;
    }

    public void CameraShake(CameraShakeData data, CinemachineImpulseSource impulseSource)
    {
        if (impulseListener == null) return;
        
        SetUpShakeData(data, impulseSource);
        
        impulseSource.GenerateImpulseWithForce(data.impactForce);
    }
    

    private void SetUpShakeData(CameraShakeData data, CinemachineImpulseSource impulseSource)
    {
        impulseDefinition = impulseSource.m_ImpulseDefinition;

        impulseDefinition.m_ImpulseDuration = data.impactTime;
        impulseSource.m_DefaultVelocity = data.defaultVelocity;
        impulseDefinition.m_CustomImpulseShape = data.impulseCurve;

        impulseListener.m_ReactionSettings.m_AmplitudeGain = data.ListenerAmplitude;
        impulseListener.m_ReactionSettings.m_FrequencyGain = data.ListenerFrequency;
        impulseListener.m_ReactionSettings.m_Duration = data.ListenerDuration;
    }
}
