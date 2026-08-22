using UnityEngine;


[CreateAssetMenu(menuName = "Combat/AttackSequences")]
public class AttackSequences : ScriptableObject
{
    public string sequenceName;
    public AttackDefinition[] steps;
    public float weight = 1f;
}
