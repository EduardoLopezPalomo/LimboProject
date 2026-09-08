using UnityEngine;

public class TutorialExitBlocker : MonoBehaviour
{
    [SerializeField] private KillGoal actualGoal;
    [SerializeField] private Collider colliderBloqueo;   // sólido, bloquea la salida físicamente
    [SerializeField] private Collider colliderAviso;      // trigger, un poco antes del bloqueo
    [SerializeField] private string tituloNotificacion = "Aún quedan enemigos por derrotar";
    [SerializeField] private float cooldownAviso = 5f;    // evita spamear la notificación si te quedas ahí

    private float ultimoAviso = -999f;

    private void OnEnable()
    {
        Goals.OnGoalCompleted += RevisarEstado;
    }

    private void OnDisable()
    {
        Goals.OnGoalCompleted -= RevisarEstado;
    }

    private void Start()
    {
        RevisarEstado(); // por si ya estaba completada la goal al cargar la escena
    }

    private void RevisarEstado()
    {
        if (actualGoal.completed)
            colliderBloqueo.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (actualGoal.completed) return;
        if (Time.time - ultimoAviso < cooldownAviso) return;

        ultimoAviso = Time.time;
        GameplayWarning.Show(tituloNotificacion);
    }
}