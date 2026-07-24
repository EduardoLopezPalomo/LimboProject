using System;
using UnityEngine;

[Serializable]
public struct CameraShakeData
{
    [Header("Impulse Source Settings")]
    public float impactTime;
    public float impactForce;
    public Vector3 defaultVelocity;
    public AnimationCurve impulseCurve;
    
    [Header("Impulse Listener Settings")]
    public float ListenerAmplitude;
    public float ListenerFrequency;
    public float ListenerDuration;
}
