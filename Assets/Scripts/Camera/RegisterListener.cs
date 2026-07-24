using Cinemachine;
using UnityEngine;

public class RegisterListener : MonoBehaviour
{
    private CinemachineImpulseListener impulseListener;

    void Start()
    {
        impulseListener = GetComponent<CinemachineImpulseListener>();
        CameraShakeManager.instance.SetImpulseListener(impulseListener);
    }
}
