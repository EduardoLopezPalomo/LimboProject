using UnityEngine;

[System.Serializable]
public class PlayerData 
{
    public float[] position;
    public float health;
    
    public PlayerData() { }

    public PlayerData(GameObject player)
    {
        position = new float[3];
        position[0] = player.transform.position.x;
        position[1] = player.transform.position.y;
        position[2] = player.transform.position.z;
        
        AttributeSystem attributeSystem = player.GetComponent<AttributeSystem>();
        health = attributeSystem.GetAttributeValue("PlayerHealth");
    }
}
