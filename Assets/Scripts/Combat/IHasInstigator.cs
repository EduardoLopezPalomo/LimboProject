using UnityEngine;

/// <summary>
/// Implemented by damage sources that exist because somebody created them - an arrow, a fireball, a
/// ground explosion - and therefore can name the character behind them.
///
/// A hazard that cannot say who spawned it can still hurt the player, but a defensive action has
/// nothing to pay back: <c>DefenseState.ResolveHit()</c> answers
/// <see cref="HitResult.PerfectParry"/> and the hazard has no target to punish, so the parry is
/// silently rewarded with nothing. Whoever spawns the hazard (CharacterVFX, the movement script a
/// behaviour graph hands the owner to, or a plain Instantiate) is expected to call
/// <see cref="HazardInstigator.Assign"/> right after the object is created/activated.
/// </summary>
public interface IHasInstigator
{
    /// <summary>
    /// Sets the character responsible for this hazard. Called after activation, never before
    /// <c>OnEnable</c>: pooled hazards clear their instigator when they are switched off, so a
    /// recycled explosion can never keep punishing the character that spawned the previous one.
    /// A null argument means "nobody claims this hazard" and is safe.
    /// </summary>
    void SetInstigator(GameObject instigator);
}

/// <summary>
/// One-line way for spawners to hand their GameObject to every damage component of what they just
/// created, without knowing which of them (LightingDamage, ExplosionDamage, ...) the prefab carries.
/// </summary>
public static class HazardInstigator
{
    /// <summary>
    /// Pushes <paramref name="instigator"/> onto every <see cref="IHasInstigator"/> under
    /// <paramref name="root"/>, including inactive children. Silent on a root with no receivers:
    /// most VFX are pure decoration and have nothing to be told.
    /// </summary>
    public static void Assign(GameObject root, GameObject instigator)
    {
        if (root == null) return;

        IHasInstigator[] receivers = root.GetComponentsInChildren<IHasInstigator>(true);
        for (int i = 0; i < receivers.Length; i++)
        {
            receivers[i].SetInstigator(instigator);
        }
    }
}
