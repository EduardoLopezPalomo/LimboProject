using UnityEngine;

public class ParticleController : MonoBehaviour
{
    public GameObject particlePrefab;

    public void ActivateParticle()
    {
        particlePrefab.SetActive(true);
    }

    public void DeactivateParticle()
    {
        particlePrefab.SetActive(false);
    }
}
