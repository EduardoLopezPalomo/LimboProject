using UnityEngine;

public class KarmaManager : MonoBehaviour
{
    public static KarmaManager Instance { get; private set; }

    [Range(-100, 100)] public int KarmaPoints { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddKarma(int points)
    {
        KarmaPoints = Mathf.Clamp(KarmaPoints + points, -100, 100);
        Debug.Log("Karma Points: " + KarmaPoints);
    }
}
