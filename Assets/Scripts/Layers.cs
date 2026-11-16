using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The one place first-party code turns a layer NAME into a layer.
///
/// Unity resolves a name by looking it up in Project Settings &gt; Tags and Layers, and when the name is
/// not there it does not complain: <see cref="LayerMask.NameToLayer"/> answers -1 and
/// <see cref="LayerMask.GetMask"/> answers 0. Both are inert rather than fatal, so renaming a layer in
/// the Project Settings window silently rewrites gameplay. `other.gameObject.layer == NameToLayer("Player")`
/// stops matching forever (no layer is -1), an OverlapSphere filtered by a missing mask finds nobody, and
/// the game carries on as if nothing happened. Enemies simply stop seeing the player.
///
/// Going through here instead means the same rename is reported once, by name, and says what it broke:
/// <see cref="MaskOf"/> returns a mask that matches nothing and <see cref="IndexOf"/> returns -1, so the
/// behaviour is unchanged (still inert) but it is no longer invisible. That is the difference between a
/// confusing bug report and a one-line answer.
///
/// The names below are spelled exactly as the Project Settings window spells them - including
/// <see cref="EnemyLayer"/>, whose "Layer" suffix is part of the name, not of the code - because the
/// whole point is that the string a designer edits and the string a programmer reads are the same one.
/// Note "EnemyLayer" the LAYER is not "Enemy" the TAG: both exist, and <see cref="DamageResolver.TagExists"/>
/// is the tag-side equivalent of this guard.
///
/// Answers are cached for the life of the run. A layer rename is therefore picked up by the next domain
/// reload or restart, not mid-play, and a genuinely missing layer logs exactly one error rather than one
/// per swing of every weapon.
/// </summary>
public static class Layers
{
    // The names this project's Project Settings defines and that code actually asks for.
    // Built-in layers are included because the map tooling culls by them.
    public const string Default = "Default";
    public const string Water = "Water";
    public const string UI = "UI";
    public const string Wall = "Wall";
    public const string EnemyLayer = "EnemyLayer";
    public const string Player = "Player";
    public const string Floor = "Floor";
    public const string Mountain = "Mountain";

    /// <summary>
    /// Every name the resolver is asked for, so it can be checked in one go instead of waiting for the
    /// code path that would have found out. <c>Tools/Ciro/Validate Ability References</c> (check 8)
    /// verifies each of these against Project Settings, which is what turns "rename a layer" from a
    /// silent gameplay change into a reported problem before anyone has to play for it.
    /// </summary>
    public static readonly string[] Known =
    {
        Default, Water, UI, Wall, EnemyLayer, Player, Floor, Mountain,
    };

    /// <summary>Resolved layer index per name; a -1 is a cached "this name does not exist".</summary>
    static readonly Dictionary<string, int> Resolved = new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Names we have already reported, so a missing layer costs one error and not one per call.</summary>
    static readonly HashSet<string> Reported = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// The layer index <paramref name="layerName"/> refers to, or -1 when Project Settings has no such
    /// layer. Reports the missing name the first time it is asked for.
    /// </summary>
    public static int IndexOf(string layerName)
    {
        int index;
        if (TryIndex(layerName, out index)) return index;
        Report(layerName);
        return -1;
    }

    /// <summary>
    /// The layer index <paramref name="layerName"/> refers to, saying nothing when it does not exist.
    /// For a caller that has its own, better message about the consequence - PlayerWeaponDamage disables
    /// the weapon, and that is the line a designer needs to read.
    /// </summary>
    public static bool TryIndex(string layerName, out int index)
    {
        index = -1;
        if (string.IsNullOrEmpty(layerName)) return false;

        if (Resolved.TryGetValue(layerName, out index)) return index >= 0;

        index = LayerMask.NameToLayer(layerName);
        Resolved[layerName] = index;
        return index >= 0;
    }

    /// <summary>
    /// True when <paramref name="layer"/> (a <c>gameObject.layer</c>) is the layer
    /// <paramref name="layerName"/> names. False - and reported - when the name does not exist, which is
    /// the same answer `layer == LayerMask.NameToLayer(name)` would have given, except out loud.
    /// </summary>
    public static bool Is(int layer, string layerName)
    {
        int index;
        if (TryIndex(layerName, out index)) return layer == index;
        Report(layerName);
        return false;
    }

    /// <summary>
    /// A single-layer mask for <paramref name="layerName"/>. When the name does not exist the mask is 0,
    /// which matches NOTHING: an intentional choice over "everything", because a physics query that cannot
    /// filter must not go on to hit the whole level. Inert, but now reported.
    /// </summary>
    public static LayerMask MaskOf(string layerName)
    {
        int index;
        if (TryIndex(layerName, out index)) return 1 << index;
        Report(layerName);
        return 0;
    }

    /// <summary>
    /// A mask of several named layers; a name that does not exist contributes nothing and is reported.
    /// For the map tooling, which culls the capture down to the layers that depict ground.
    /// </summary>
    public static LayerMask MaskOf(params string[] layerNames)
    {
        var mask = 0;
        if (layerNames == null) return mask;
        for (int i = 0; i < layerNames.Length; i++)
        {
            int index;
            if (TryIndex(layerNames[i], out index)) mask |= 1 << index;
            else Report(layerNames[i]);
        }
        return mask;
    }

    /// <summary>
    /// Checks every name in <see cref="Known"/> once, at start-up, so a renamed layer is on the console
    /// before the first swing rather than at the moment an ability happens to run.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ValidateAtStart()
    {
        for (int i = 0; i < Known.Length; i++) IndexOf(Known[i]);
    }

    static void Report(string layerName)
    {
        if (string.IsNullOrEmpty(layerName)) return;
        if (!Reported.Add(layerName)) return;

        Debug.LogError("[Layers] The layer \"" + layerName + "\" does not exist in Project Settings > Tags " +
            "and Layers, so everything that asks for it now matches NOTHING: area attacks find no targets, " +
            "weapon and projectile hits are never registered, and the affected code keeps running without " +
            "saying so. Restore the name, or update Assets/Scripts/Layers.cs to the new one. The name is " +
            "resolved once per run, so a fix needs a restart (or a script reload in the Editor).");
    }
}
