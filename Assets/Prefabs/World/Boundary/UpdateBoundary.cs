using UnityEngine;

public class UpdateBoundary : MonoBehaviour
{
    public Transform player;
    public Material BoundaryMaterial;

    void Update()
    {
        BoundaryMaterial.SetVector("_PlayerLocation", player.position);
    }
}
