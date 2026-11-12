/// <summary>
/// What a map marker is allowed to do after it has been placed. Locked by
/// D-11 Art map display model Q1: three tiers on one map, north-up, one marker layer.
/// </summary>
public enum MapMarkerTier
{
    /// <summary>A fixed place - a shop, a portal, a camp. Never moves, never rotates.</summary>
    Static = 0,

    /// <summary>An enemy or an NPC. Tracks its host's position, never its rotation.</summary>
    Dynamic = 1,

    /// <summary>The player. Tracks position and horizontal rotation (yaw).</summary>
    Important = 2
}
