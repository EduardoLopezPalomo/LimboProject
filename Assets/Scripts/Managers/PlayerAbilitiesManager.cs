using UnityEngine;

public class PlayerAbilitiesManager : MonoBehaviour
{
    public static PlayerAbilitiesManager Instance { get; private set; }

    public AbilityDataBasePlayer abilityDatabase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
