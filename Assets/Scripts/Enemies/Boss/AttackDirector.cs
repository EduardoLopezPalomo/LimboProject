using System.Linq;
using UnityEngine;

public class AttackDirector : MonoBehaviour
{
    [SerializeField] private AttackLibrary library;
    [SerializeField] private PhaseController phaseController;
    [SerializeField] private float repeatPenaltyMultiplier = 0.5f;

    private AttackSequences LastUsedSequence;

    public AttackSequences PickSequence()
    {
        AttackSequences[] options = library.GetSequencesForPhase(phaseController.CurrentPhase);
        if (options == null || options.Length == 0) return null;
        
        float total = 0f;
        float[] effectiveWeights = new float[options.Length];
        
        for (int i = 0; i < options.Length; i++)
        {
            float w = options[i].weight;
            if (options[i] == LastUsedSequence)
            {
                w *= repeatPenaltyMultiplier;
            }
            effectiveWeights[i] = w;
            Debug.Log(effectiveWeights[i]);
            total += w;
        }

        float roll = Random.value * total;
        Debug.Log(roll);
        for (int i = 0; i < options.Length; i++)
        {
            if (roll < effectiveWeights[i])
            {
                LastUsedSequence = options[i];
                return options[i];
            }
            roll -= effectiveWeights[i];
        }

        LastUsedSequence = options[^1];
        return options[^1];
    }
}
