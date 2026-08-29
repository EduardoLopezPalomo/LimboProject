using System.Collections;
using TMPro;
using UnityEngine;

public class LoadNotification : MonoBehaviour
{
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
        SaveSystem.OnFailLoadGame += LoadGame;
        GameLoader.OnGameLoadFail += LoadGame;
    }

    void OnDisable()
    {
        SaveSystem.OnFailLoadGame -= LoadGame;
        GameLoader.OnGameLoadFail -= LoadGame;
    }

    private void LoadGame(string message)
    {
        if (showNotificationCo == null)
        {
            notificationText.text = message;
            showNotificationCo = StartCoroutine(ShowNotification());
        }
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
