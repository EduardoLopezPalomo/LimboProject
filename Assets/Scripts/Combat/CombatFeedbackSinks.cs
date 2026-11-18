using Cinemachine;
using UnityEngine;

/// <summary>
/// The camera-shake half of combat feedback (T-010, P1.3 step 2).
///
/// <see cref="DamageResolver"/> reaches feedback only through these three narrow sinks so the
/// combat pipeline can be exercised without the manager singletons that implement them: a test
/// assigns a recorder, the game keeps the scene managers. The default binding lives in
/// <see cref="SceneFeedbackSinks"/>; assigning null at runtime is tolerated (the hot path falls
/// back to the default), so a leaked test value cannot break gameplay.
///
/// This file is a permanent resident of the future Ciro.Combat assembly ([[D-04]] staging step 3):
/// it must never reference a manager type, only the data already flowing through the resolver.
/// </summary>
public interface IShakeSink
{
    void Shake(CameraShakeData data, CinemachineImpulseSource impulseSource);
}

/// <summary>The controller-rumble half of combat feedback. See <see cref="IShakeSink"/>.</summary>
public interface IRumbleSink
{
    void Vibrate(float lowFrequency, float highFrequency, float duration);
}

/// <summary>
/// The sound half of combat feedback. Takes the whole <see cref="SFXDefinition"/> because that is
/// what every caller already holds; unpacking it into manager parameters stays on the default
/// binding side. See <see cref="IShakeSink"/>.
/// </summary>
public interface ISfxSink
{
    void PlayRandomPitch(SFXDefinition def, Transform source);
}
