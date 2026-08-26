using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class LevelLoader : MonoBehaviour
{
    public Animator animator;
    public float transitionTime = 1f;
    public float MusicFadeTime = 2f;
    
    public static LevelLoader Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadLevelAsync(int index, System.Action onLoaded = null, bool useAttributeCache = true)
    {
        StartCoroutine(LoadLevelAsyncCoroutine(index, onLoaded, useAttributeCache));
    }
    
    private PlayerInput GetPlayerInput()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<PlayerInput>() : null;
    }

    private void SavePlayerAttributes()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        player?.GetComponent<PlayerAttributesPersistance>()?.SaveCurrentState();
    }

    private void SetPlayerAttributes()
    {
        GameObject newPlayer = GameObject.FindGameObjectWithTag("Player");
        newPlayer?.GetComponent<PlayerAttributesPersistance>()?.RestoreState();
    }

    IEnumerator LoadLevelAsyncCoroutine(int levelIndex,  System.Action onLoaded, bool useAttributeCache)
    {
        animator.SetTrigger("StartLoading");
        GetPlayerInput()?.DeactivateInput();
        if (useAttributeCache) SavePlayerAttributes();
        float MaxVolume = SoundMixerManager.instance.GetMasterVolume();
        SoundMixerManager.instance.ParallelFadeVolume(MixerNames.masterVolume,-80f, MusicFadeTime);
        
        yield return new WaitForSecondsRealtime(transitionTime);
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(levelIndex);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        SoundMixerManager.instance.ParallelFadeVolume(MixerNames.masterVolume, MaxVolume, MusicFadeTime);
        animator.SetTrigger("EndLoading");
        if (useAttributeCache) SetPlayerAttributes();
        onLoaded?.Invoke();
        GetPlayerInput()?.ActivateInput();
    }
}
