using UnityEngine;

public class QuestZoneTrigger : MonoBehaviour
{
    [SerializeField] private EstadoMision estadoRequerido;
    [SerializeField] private EstadoMision estadoAlCompletar;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!QuestManager.Instance.EstaEnEstado(estadoRequerido)) return;

        QuestManager.Instance.AvanzarA(estadoAlCompletar);
    }
}
