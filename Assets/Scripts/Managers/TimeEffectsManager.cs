using System.Collections;
using UnityEngine;

public static class TimeEffectsManager 
{
    private static bool isPaused = false;
    private static CoroutineRunner runner;
    private static float defaultTimeScale = 1f;

    /// <summary>
    /// T-058 M4: the read-only half of the latch. There is still exactly one boolean and no owner
    /// concept - deciding who may pause whom is decision D-18, not this line - but a caller that is
    /// about to pause now has a way to ask whether somebody else already has the world stopped, which
    /// is the difference between "the map opens" and "the map opens and the next menu to close hands
    /// the clock back while it is still on screen" (MapToggle.CanOpenMap). Read-only on purpose: a
    /// setter here would be a second latch wearing the first one's clothes.
    /// </summary>
    public static bool IsPaused => isPaused;

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
