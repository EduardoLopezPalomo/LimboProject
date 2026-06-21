using UnityEngine;

public enum Modifier
{
    Add,
    Multiply,
    Override
}
public enum EffectTime
{
    Instant,
    Infinite,
    Duration
}
/*
 *  It is the data class for gameplay effects
 *  It only has de atrribute that want to change, the value, type of modifier,
 *  the type of time that is modified and the duration if needed
 */
[CreateAssetMenu(menuName = "AbilitySystem/Effect")]
public class GameplayEffect : ScriptableObject
{
    public GameplayAttributeData TargetAttribute;
    public float Value;
    public Modifier Modifier;

    public EffectTime EffectTime;
    public float Duration;
    public bool CanAttributeResponse = true;
}
