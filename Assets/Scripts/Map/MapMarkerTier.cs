/// <summary>
/// What a map marker is allowed to do after it has been placed. Locked by
/// D-11 Art map display model Q1: three tiers on one map, north-up, one marker layer.
///
/// Renamed 11/15/2026: tier 2 was "Important", which described a feeling instead of the one
/// thing that may use it - the player. Only the member is renamed; the underlying value is still
/// 2, so every authored marker keeps working (Unity stores enums as ints, and exactly one marker
/// in the project holds a 2: Assets/Characters/Player.prefab).
/// </summary>
public enum MapMarkerTier
{
    /// <summary>A fixed place - a shop, a portal, a camp. Never moves, never rotates.</summary>
    Static = 0,

    /// <summary>An enemy or an NPC. Tracks its host's position, never its rotation.</summary>
    Dynamic = 1,

    /// <summary>The player. Tracks position and horizontal rotation (yaw). Nothing else should use
    /// this tier: rotation is computed for it and for nothing else, so putting it on an enemy
    /// gives a pin that spins but never walks.</summary>
    Player = 2
}
