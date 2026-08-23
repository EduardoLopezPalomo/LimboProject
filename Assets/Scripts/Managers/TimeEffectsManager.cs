using System.Collections;
using UnityEngine;

public static class TimeEffectsManager 
{
    private static bool isPaused = false;
    private static CoroutineRunner runner;
    private static float defaultTimeScale = 1f;

    public static void PauseGame()
    {
        if(!isPaused)
        {
            EnsureRunner();
            runner.CancelActiveSlowMo();
            
            Time.timeScale = 0f;
            isPaused = true;
        }
    }
    
    public static void ResumeGame()
    {
        if(isPaused)
        {
            Time.timeScale = defaultTimeScale;
            isPaused = false;
        }
    }
    
    public static void DoParrySlowMotion(float scale, float duration, float recoverDuration)
    {
        if (isPaused) return; 

        EnsureRunner();
        runner.StartSlowMo(scale, duration, recoverDuration);
    }
    
    private static void EnsureRunner()
    {
        if (runner != null) return;

        var go = new GameObject("TimeEffectsRunner");
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<CoroutineRunner>();
    }
    
    private class CoroutineRunner : MonoBehaviour
    {
        private Coroutine activeSlowMo;

        public void StartSlowMo(float scale, float duration, float recoverDuration)
        {
            if (activeSlowMo != null)
            {
                StopCoroutine(activeSlowMo);
            }
            activeSlowMo = StartCoroutine(SlowMoRoutine(scale, duration, recoverDuration));
        }
        
        public void CancelActiveSlowMo()
        {
            if (activeSlowMo != null)
            {
                StopCoroutine(activeSlowMo);
                activeSlowMo = null;
            }
        }
        
        private IEnumerator SlowMoRoutine(float scale, float duration, float recoverDuration)
        {
            Time.timeScale = scale;

            yield return new WaitForSecondsRealtime(duration);

            float elapsed = 0f;
            float startScale = Time.timeScale;
            while (elapsed < recoverDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(startScale, 1f, elapsed / recoverDuration);
                yield return null;
            }

            Time.timeScale = 1f;
            activeSlowMo = null;
        }
    }
}
