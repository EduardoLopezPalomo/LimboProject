using UnityEngine;
using UnityEngine.Serialization;

public class TutorialExitBlocker : MonoBehaviour
{
    [FormerlySerializedAs("actualGoal")]
    [SerializeField] private KillGoal currentGoal;
    [FormerlySerializedAs("colliderBloqueo")]
    [SerializeField] private Collider blockCollider;   // sólido, bloquea la salida físicamente
    [FormerlySerializedAs("colliderAviso")]
    [SerializeField] private Collider warningTrigger;      // trigger, un poco antes del bloqueo
    [FormerlySerializedAs("tituloNotificacion")]
    [SerializeField] private string notificationTitle = "Aún quedan enemigos por derrotar";
    [FormerlySerializedAs("cooldownAviso")]
    [SerializeField] private float warningCooldown = 5f;    // evita spamear la notificación si te quedas ahí

    private float lastWarningTime = -999f;

    private void OnEnable()
    {
        Goals.OnGoalCompleted += CheckState;
    }

    private void OnDisable()
    {
        Goals.OnGoalCompleted -= CheckState;
    }

    private void Start()
    {
        CheckState(); // por si ya estaba completada la goal al cargar la escena
    }

    private void CheckState()
    {
        if (currentGoal.completed)
            blockCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (currentGoal.completed) return;
        if (Time.time - lastWarningTime < warningCooldown) return;

        lastWarningTime = Time.time;
        GameplayWarning.Show(notificationTitle);
    }
}