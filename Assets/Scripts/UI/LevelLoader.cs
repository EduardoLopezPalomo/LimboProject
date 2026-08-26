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
    
    public void LoadLevel(int index)
    {
       StartCoroutine(LoadLevelCoroutine(index));
    }

    public void LoadLevelAsync(int index, System.Action onLoaded)
    {
        StartCoroutine(LoadLevelAsyncCoroutine(index, onLoaded));
    }
    
    private PlayerInput GetPlayerInput()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<PlayerInput>() : null;
    }

    IEnumerator LoadLevelCoroutine(int levelIndex)
    {
        animator.SetTrigger("StartLoading");
        GetPlayerInput()?.DeactivateInput();
        float MaxVolume = SoundMixerManager.instance.GetMasterVolume();
        SoundMixerManager.instance.ParallelFadeVolume(MixerNames.masterVolume,-80f, MusicFadeTime);
        
        yield return new WaitForSecondsRealtime(transitionTime);
        
        animator.SetTrigger("EndLoading");
        SoundMixerManager.instance.ParallelFadeVolume(MixerNames.masterVolume, MaxVolume, MusicFadeTime);
        SceneManager.LoadScene(levelIndex);
        GetPlayerInput()?.ActivateInput();
    }

    IEnumerator LoadLevelAsyncCoroutine(int levelIndex,  System.Action onLoaded)
    {
        animator.SetTrigger("StartLoading");
        GetPlayerInput()?.DeactivateInput();
        float MaxVolume = SoundMixerManager.instance.GetMasterVolume();
        SoundMixerManager.instance.ParallelFadeVolume(MixerNames.masterVolume,-80f, MusicFadeTime);
        
        yield return new WaitForSecondsRealtime(transitionTime);
        
        SoundMixerManager.instance.ParallelFadeVolume(MixerNames.masterVolume, MaxVolume, MusicFadeTime);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(levelIndex);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        animator.SetTrigger("EndLoading");
        onLoaded?.Invoke();
        GetPlayerInput()?.ActivateInput();
    }
}
