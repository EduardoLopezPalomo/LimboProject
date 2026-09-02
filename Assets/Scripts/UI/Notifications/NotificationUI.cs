using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NotificationUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text notificationTypeText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private SFXDefinition notificationSound;
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Config por tipo (opcional)")]
    [SerializeField] private List<NotificationTypeConfig> configuracionesPorTipo;
    [SerializeField] private float duracionVisiblePorDefecto = 3f; 

    private void Start()
    {
        canvasGroup.alpha = 0f;
    }

    public IEnumerator ShowNotification(NotificationData data)
    {
        titleText.text = data.title;
        notificationTypeText.text = GetNotificationType(data.notificationType);

        SFXDefinition sonido = ObtenerSonidoParaTipo(data.notificationType); // <-- NUEVO
        if (sonido != null)
            SoundFXManager.instance.PlaySound(sonido.sfx, transform, sonido.volume); // <-- usa "sonido" en vez de "notificationSound"

        yield return StartCoroutine(Fade(0f, 1f));
    }

    public IEnumerator HideNotification()
    {
        yield return StartCoroutine(Fade(1f, 0f));
    }

    public float ObtenerDuracionVisible(NotificationType tipo)
    {
        var config = configuracionesPorTipo.Find(c => c.type == tipo);
        return config != null ? config.duracionVisible : duracionVisiblePorDefecto;
    }

    private SFXDefinition ObtenerSonidoParaTipo(NotificationType tipo)
    {
        var config = configuracionesPorTipo.Find(c => c.type == tipo);
        if (config != null && config.sonido != null) return config.sonido;
        return notificationSound;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private string GetNotificationType(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.GoalCompleted:
                return "Goal completed";
            case NotificationType.QuestCompleted:
                return "Mision completed";
            case NotificationType.NewQuest:
                return "New quest";
        }

        return "";
    }

    [System.Serializable]
    public class NotificationTypeConfig
    {
        public NotificationType type;
        public SFXDefinition sonido;
        public float duracionVisible = 3f;
    }
}
