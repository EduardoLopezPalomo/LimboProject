using System.Linq;
using UnityEngine;

public class AttackDirector : MonoBehaviour
{
    [SerializeField] private AttackLibrary library;
    [SerializeField] private PhaseController phaseController;

    public AttackSequences PickSequence()
    {
        AttackSequences[] options = library.GetSequencesForPhase(phaseController.CurrentPhase);
        if (options == null || options.Length == 0) return null;

        float total = options.Sum( s => s.weight);
        float roll = Random.value * total;
        foreach (AttackSequences seq in options)
        {
            if (roll < seq.weight) return seq;
            roll -= seq.weight;
        }
        return options[^1];
    }
}
