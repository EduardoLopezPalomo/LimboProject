using System;
using Cinemachine;
using UnityEngine;

public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager instance;
    
    [SerializeField] private CinemachineImpulseListener impulseListener;
    
    private CinemachineImpulseDefinition impulseDefinition;

    // The "no listener" state is a scene-configuration problem, not a per-hit one: without a rate limit a
    // busy combat scene prints one line per hit and buries the message it is trying to deliver (T-007).
    private bool warnedMissingListener;

    /// <summary>
    /// Whether a shake has somewhere to land. <see cref="CameraShake"/> silently drops the shake when this is
    /// false, so tools and tests can ask instead of guessing from the private field.
    /// </summary>
    public bool HasImpulseListener => impulseListener != null;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    /// <summary>
    /// Sets the listener every shake is read through. Passing <c>null</c> is the supported way to switch
    /// camera shake off for the rest of the scene; the next shake reports it once (see
    /// <see cref="HasImpulseListener"/>). Registering a listener clears the warning so a later loss is
    /// reported again.
    /// </summary>
    public void SetImpulseListener(CinemachineImpulseListener listener)
    {
        impulseListener = listener;

        if (listener != null)
        {
            warnedMissingListener = false;
        }
    }

    /// <summary>
    /// Generates an impulse with <paramref name="data"/> applied to both the source and the listener.
    ///
    /// <b>Concurrent shakes:</b> <see cref="SetUpShakeData"/> writes the reaction settings of the ONE shared
    /// listener, so when two hits land in the same frame the <b>last writer</b> decides the amplitude the
    /// player feels. The impulses themselves still compose through Cinemachine's own blending; only the
    /// listener reaction is shared. This is a deliberate, honest trade — making the reaction per-source would
    /// mean moving those settings onto all 25 impulse sources, i.e. every weapon and hazard in the game.
    /// </summary>
    public void CameraShake(CameraShakeData data, CinemachineImpulseSource impulseSource)
    {
        if (impulseSource == null)
        {
            Debug.LogError("CameraShakeManager::CameraShake: no impulse source was passed, so this shake cannot " +
                "be generated. Pass the CinemachineImpulseSource that owns the cue (used to throw a " +
                "NullReferenceException from here).", this);
            return;
        }

        if (impulseListener == null)
        {
            WarnMissingListenerOnce(impulseSource);
            return;
        }
        
        SetUpShakeData(data, impulseSource);
        
        impulseSource.GenerateImpulseWithForce(data.impactForce);
    }

    private void WarnMissingListenerOnce(CinemachineImpulseSource impulseSource)
    {
        if (warnedMissingListener) return;

        warnedMissingListener = true;
        Debug.LogError("CameraShakeManager: no impulse listener is registered, so camera shake is disabled and " +
            $"the shake requested by '{impulseSource.name}' was dropped. A virtual camera needs a " +
            "CinemachineImpulseListener plus RegisterListener (see PlayerFollowCamera.prefab). Further shake " +
            "warnings from this scene are suppressed until a listener is registered.", this);
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
