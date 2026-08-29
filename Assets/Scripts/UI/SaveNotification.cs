using System.Collections;
using TMPro;
using UnityEngine;

public class SaveNotification : MonoBehaviour
{
    public string successSaveNotification;
    public string failSaveNotification;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float notificationDuration = 3f;
    
    private Coroutine showNotificationCo;
    
    void Start()
    {
        canvasGroup.alpha = 0f;
    }

    void OnEnable()
    {
        SaveSystem.OnSaveGame += SaveGame;
    }

    void OnDisable()
    {
        SaveSystem.OnSaveGame -= SaveGame;
    }

    private void SaveGame(bool success)
    {
        if (showNotificationCo != null) return;
        
        notificationText.text = success ? successSaveNotification : failSaveNotification;
        showNotificationCo = StartCoroutine(ShowNotification());
    }

    private IEnumerator ShowNotification()
    {
        yield return StartCoroutine(Fade(0, 1));
            
        yield return new WaitForSecondsRealtime(notificationDuration);
            
        yield return StartCoroutine(Fade(1, 0));

        showNotificationCo = null;
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

}
