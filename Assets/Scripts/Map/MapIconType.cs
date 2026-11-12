/// <summary>
/// The vocabulary of drawings in Assets/UI Images/Map/Elements. Which sprite a value resolves
/// to is data, held by <see cref="MapIconCatalog"/> - never a switch in code (Working Agreement
/// section 5: ScriptableObject data over code).
/// </summary>
public enum MapIconType
{
    None = 0,

    // Dynamic tier.
    Enemy = 1,
    MonsterNpc = 2,
    HumanNpc = 3,

    // Important tier.
    MainCharacter = 4,

    // Static tier: quest and world places.
    MissionPoint = 10,
    Gods = 11,
    Teleport = 12,
    BossCave = 13,
    Camp = 14,
    Fire = 15,
    DeadTree = 16,
    TreeStump = 17,
    Cage = 18,
    Boxes = 19,

    // Static tier: the six market stalls.
    FishMarket = 20,
    ForgeMarket = 21,
    LuxuryMarket = 22,
    MagicMarket = 23,
    MeatMarket = 24,
    VegetableMarket = 25
}
