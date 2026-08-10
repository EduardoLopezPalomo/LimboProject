using UnityEngine;
/*  It has the data for gameplay attribute
 *  it only has a name and value
 * 
 */
[CreateAssetMenu(menuName = "AbilitySystem/Attribute")]
public class GameplayAttributeData : ScriptableObject
{
    public string Name;
    public float BaseValue;
    public float MaxValue;
    
    [Header("Recover value")]
    public bool CanRecover = false;
    public float RecoverValuePerSecond;
    public float WaitTillRecover;

    public AttributeResponse Response;
}
