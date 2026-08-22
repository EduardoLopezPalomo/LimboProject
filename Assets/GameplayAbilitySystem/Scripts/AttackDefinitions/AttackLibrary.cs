using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Boss Attack Library")]
public class AttackLibrary : ScriptableObject
{
    public AttackSequences[] phase1Sequences;
    public AttackSequences[] phase2Sequences;
    public AttackSequences[] phase3Sequences;

    public AttackSequences[] GetSequencesForPhase(BossPhase phase)
    {
        return phase switch
        {
            BossPhase.Phase1 => phase1Sequences,
            BossPhase.Phase2 => phase2Sequences,
            BossPhase.Phase3 => phase3Sequences,
            _ => phase1Sequences
        };
    }
}
