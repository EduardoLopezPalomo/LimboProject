using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public float[] position;
    public float health;
    public int slot1Id = -1;
    public int slot2Id = -1;

    public int[] AllAbilitiesIds;

    public PlayerData() { }

    public PlayerData(GameObject player)
    {
        position = new float[3];
        position[0] = player.transform.position.x;
        position[1] = player.transform.position.y;
        position[2] = player.transform.position.z;

        AttributeSystem attributeSystem = player.GetComponent<AttributeSystem>();
        health = attributeSystem.GetAttributeValue("PlayerHealth");

        AbilityHolder abilityHolder = player.GetComponent<AbilityHolder>();
        slot1Id = abilityHolder.GetEquippedAbility(1)?.Id ?? -1;
        slot2Id = abilityHolder.GetEquippedAbility(2)?.Id ?? -1;

        AllAbilitiesIds = abilityHolder.GetAbilityList().ConvertAll(ability => ability.Id).ToArray();
    }
}
