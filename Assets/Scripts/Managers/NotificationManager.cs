using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    [SerializeField] private NotificationUI notificationUI;

    private Queue<NotificationData> queue = new Queue<NotificationData>();
    private bool isShowing = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Enqueue(NotificationData data)
    {
        queue.Enqueue(data);
        if (!isShowing)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        isShowing = true;

        while (queue.Count > 0)
        {
            NotificationData current = queue.Dequeue();

            yield return StartCoroutine(notificationUI.ShowNotification(current));

            float duracion = notificationUI.ObtenerDuracionVisible(current.notificationType);
            yield return new WaitForSecondsRealtime(duracion);

            yield return StartCoroutine(notificationUI.HideNotification());
        }

        isShowing = false;
    }
}
