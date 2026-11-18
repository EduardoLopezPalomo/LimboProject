using Cinemachine;
using UnityEngine;

/// <summary>
/// The production bindings of the combat feedback sinks (T-010): each one forwards to the same
/// manager singleton <see cref="DamageResolver"/> called directly before the seam existed, with
/// the same missing-manager reports. Nothing here changes behaviour; it moves the singleton
/// touches from the resolver to these three adapters so the resolver itself becomes testable and
/// so the future Ciro.Combat assembly ([[D-04]] staging step 3) can keep the interfaces while
/// these bindings move next to the managers (an assembly cannot reference Assembly-CSharp).
///
/// Shared instances are stateless: the hot path uses them without allocating per call.
/// </summary>
public sealed class SceneShakeSink : IShakeSink
{
    public static readonly SceneShakeSink Shared = new SceneShakeSink();

    SceneShakeSink() { }

    public void Shake(CameraShakeData data, CinemachineImpulseSource impulseSource)
    {
        if (CameraShakeManager.instance == null)
        {
            Debug.LogError(DamageResolver.Label(null) + " CameraShakeManager.instance is missing from the scene; the hit's camera shake was skipped.");
            return;
        }

        CameraShakeManager.instance.CameraShake(data, impulseSource);
    }
}

public sealed class SceneRumbleSink : IRumbleSink
{
    public static readonly SceneRumbleSink Shared = new SceneRumbleSink();

    SceneRumbleSink() { }

    public void Vibrate(float lowFrequency, float highFrequency, float duration)
    {
        if (VibrationManager.Instance == null) return;

        VibrationManager.Instance.Vibrate(lowFrequency, highFrequency, duration);
    }
}

public sealed class SceneSfxSink : ISfxSink
{
    public static readonly SceneSfxSink Shared = new SceneSfxSink();

    SceneSfxSink() { }

    public void PlayRandomPitch(SFXDefinition def, Transform source)
    {
        if (SoundFXManager.instance == null)
        {
            Debug.LogError(DamageResolver.Label(null) + $" SoundFXManager.instance is missing from the scene; the sound '{def.name}' was skipped.");
            return;
        }

        SoundFXManager.instance.PlaySoundRandomPitch(def.sfx, source, def.volume, def.minPitch, def.maxPitch);
    }
}
