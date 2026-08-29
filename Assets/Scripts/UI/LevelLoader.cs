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
    
    public void LoadLevelAndTeleportAsync(int index, Vector3 point, Quaternion rotation)
    {
        StartCoroutine(LoadLevelAndTeleportAsyncCoroutine(index, point, rotation));
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
        player?.GetComponent<PlayerGASPersistance>()?.SaveState();
    }

    private void SetPlayerAttributes()
    {
        GameObject newPlayer = GameObject.FindGameObjectWithTag("Player");
        newPlayer?.GetComponent<PlayerGASPersistance>()?.RestoreState();
    }

    private void TeleportPlayer(Vector3 point, Quaternion rotation)
    {
        GameObject newPlayer = GameObject.FindGameObjectWithTag("Player");
        Debug.Log($"[Teleport] Player encontrado: {newPlayer?.name}, destino: {point}");

        CharacterController cc = newPlayer.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        newPlayer.transform.SetPositionAndRotation(point, rotation);
        Debug.Log($"[Teleport] Posición inmediatamente después de setear: {newPlayer.transform.position}");

        if (cc != null) cc.enabled = true;
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
    
    IEnumerator LoadLevelAndTeleportAsyncCoroutine(int levelIndex, Vector3 point, Quaternion rotation)
    {
        animator.SetTrigger("StartLoading");
        GetPlayerInput()?.DeactivateInput();
        float MaxVolume = SoundMixerManager.instance.GetMasterVolume();
        SoundMixerManager.instance.ParallelFadeVolume(MixerNames.masterVolume,-80f, MusicFadeTime);
        
        yield return new WaitForSecondsRealtime(transitionTime);
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(levelIndex);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        TeleportPlayer(point, rotation);
        SoundMixerManager.instance.ParallelFadeVolume(MixerNames.masterVolume, MaxVolume, MusicFadeTime);
        animator.SetTrigger("EndLoading");
        GetPlayerInput()?.ActivateInput();
    }
}
