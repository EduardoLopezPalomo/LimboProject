using Cinemachine;
using UnityEngine;

/// <summary>
/// Publishes this camera's <see cref="CinemachineImpulseListener"/> to <see cref="CameraShakeManager"/> so
/// shakes have somewhere to land.
///
/// This script lives on <c>PlayerFollowCamera.prefab</c>, which is placed in scenes that do NOT all carry
/// <c>GlobalManager.prefab</c> — and <c>CameraShakeManager</c> only exists on that prefab, assigning its
/// singleton in <c>Awake</c>. Verified 11/15/2026 by GUID scan of every scene: <c>Life_totem</c>, <c>Map</c>,
/// <c>Sky_totem</c> and <c>Space_totem</c> ship this camera with no manager, so the reference used to be null
/// and the unguarded call below threw a NullReferenceException on scene start (T-003). A scene that cannot
/// shake must say so once and carry on playing.
/// </summary>
public class RegisterListener : MonoBehaviour
{
    private CinemachineImpulseListener impulseListener;

    void Start()
    {
        impulseListener = GetComponent<CinemachineImpulseListener>();

        if (impulseListener == null)
        {
            Debug.LogError($"{name}: no CinemachineImpulseListener component on this GameObject, so camera shake " +
                "cannot register. Add the component to this camera, or remove this script.", this);
            return;
        }

        if (CameraShakeManager.instance == null)
        {
            Debug.LogError($"{name}: no CameraShakeManager in this scene (it lives on GlobalManager.prefab), so " +
                "camera shake is disabled here. Add GlobalManager to the scene if it is meant to shake.", this);
            return;
        }

        CameraShakeManager.instance.SetImpulseListener(impulseListener);
    }
}
