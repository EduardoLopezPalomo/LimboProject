using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class NotificationUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text notificationTypeText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private SFXDefinition notificationSound;
    [SerializeField] private float fadeDuration = 0.3f;

    private void Start()
    {
        canvasGroup.alpha = 0f;
    }

    public IEnumerator ShowNotification(NotificationData data)
    {
        titleText.text = data.title;
        notificationTypeText.text = GetNotificationType(data.notificationType);
        
        SoundFXManager.instance.PlaySound(notificationSound.sfx, transform, notificationSound.volume);
        
        yield return StartCoroutine(Fade(0f, 1f));
    }

    public IEnumerator HideNotification()
    {
        yield return StartCoroutine(Fade(1f, 0f));
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
}
