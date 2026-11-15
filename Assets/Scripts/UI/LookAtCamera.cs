using UnityEngine;

/// <summary>
/// Billboards this transform to the main camera, facing the same way the camera faces.
/// </summary>
public class LookAtCamera : MonoBehaviour
{
    // T-046 B2: this used to be Camera.main.transform.forward every LateUpdate. Camera.main is a
    // tagged search of the scene, so every object with this script searched for the camera 60 times
    // a second, and then wrote a forward vector it usually already had.
    private Camera sceneCamera;
    private bool warnedNoCamera;

    void LateUpdate()
    {
        if (sceneCamera == null)
        {
            sceneCamera = Camera.main;
            if (sceneCamera == null)
            {
                // Previously this threw a NullReferenceException every frame while the camera was gone.
                // Same loudness, once instead of every frame, and the object simply stops re-aiming.
                if (!warnedNoCamera)
                {
                    warnedNoCamera = true;
                    Debug.LogError($"{name} has a LookAtCamera but there is no Camera.main, so it stays at its authored rotation instead of facing the camera.", this);
                }
                return;
            }
        }

        Vector3 wanted = sceneCamera.transform.forward;

        // Writing the value an object already has still dirties its transform: only write a change.
        if ((transform.forward - wanted).sqrMagnitude > 0.0001f)
            transform.forward = wanted;
    }
}
