using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerAttributeCache
{
    public static Dictionary<string, float> Values;
    public static bool HasData;
}

public class PlayerAttributesPersistance : MonoBehaviour
{
    private AttributeSystem attributeSystem;

    private void Awake()
    {
        attributeSystem = GetComponent<AttributeSystem>();
    }

    public void RestoreState()
    {
        if (PlayerAttributeCache.HasData)
        {
            attributeSystem.ApplyValues(PlayerAttributeCache.Values, false);
        }
    }

    public void SaveCurrentState()
    {
        PlayerAttributeCache.Values = attributeSystem.CaptureValues();
        PlayerAttributeCache.HasData = true;
    }
}
