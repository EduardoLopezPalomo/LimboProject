using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class NotificationUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text notificationTypeText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Config por tipo (opcional)")]
    [FormerlySerializedAs("configuracionesPorTipo")]
    [SerializeField] private List<NotificationTypeConfig> perTypeConfigs;
    [FormerlySerializedAs("duracionVisiblePorDefecto")]
    [SerializeField] private float defaultVisibleDuration = 3f;

    private void Start()
    {
        canvasGroup.alpha = 0f;
    }

    public IEnumerator ShowNotification(NotificationData data)
    {
        titleText.text = data.title;
        notificationTypeText.text = GetNotificationType(data.notificationType);

        SFXDefinition sound = GetSoundForType(data.notificationType); // <-- NUEVO
        if (sound != null)
            SoundFXManager.instance.PlaySound(sound.sfx, transform, sound.volume); // <-- usa "sonido" en vez de "notificationSound"

        yield return StartCoroutine(Fade(0f, 1f));
    }

    public IEnumerator HideNotification()
    {
        yield return StartCoroutine(Fade(1f, 0f));
    }

    public float GetVisibleDuration(NotificationType tipo)
    {
        var config = perTypeConfigs.Find(c => c.type == tipo);
        return config != null ? config.visibleDuration : defaultVisibleDuration;
    }

    private SFXDefinition GetSoundForType(NotificationType tipo)
    {
        var config = perTypeConfigs.Find(c => c.type == tipo);
        if (config != null && config.sound != null) return config.sound;
        return null;
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
            case NotificationType.AbilityUnlocked:
                return "Ability unlocked";
        }

        return "";
    }

    [System.Serializable]
    public class NotificationTypeConfig
    {
        public NotificationType type;
        [FormerlySerializedAs("sonido")]
        public SFXDefinition sound;
        [FormerlySerializedAs("duracionVisible")]
        public float visibleDuration = 3f;
    }
}
