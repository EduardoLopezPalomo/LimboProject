using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class LevelLoader : MonoBehaviour
{
    public Animator animator;
    public float transitionTime = 1f;
    public float MusicFadeTime = 2f;

    public static LevelLoader Instance;

    // Scene names that already-written save files can still carry, mapped to the name that
    // scene has today. WorldData.sceneName stores SceneManager.GetActiveScene().name, which
    // is the scene FILE name, so renaming a scene silently breaks every save taken in it
    // (GameLoader replays that string straight into SceneManager.LoadSceneAsync).
    // Key = name stored by an older save, value = the current scene name.
    // T-038 (2026-11-10) renamed Assets/Scenes/Pueblo.unity to Assets/Scenes/Village.unity.
    private static readonly Dictionary<string, string> legacySceneNames = new Dictionary<string, string>
    {
        { "Pueblo", "Village" },
    };

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

    public void LoadLevelAndTeleportAsync(string name, Vector3 point, Quaternion rotation, bool useAttributeCache)
    {
        string sceneName = ResolveSceneName(name);
        if (sceneName == null) return;

        StartCoroutine(LoadLevelAndTeleportAsyncCoroutine(sceneName, point, rotation, useAttributeCache));
    }


    public void LoadLevelAsync(string name, System.Action onLoaded = null, bool useAttributeCache = true)
    {
        string sceneName = ResolveSceneName(name);
        if (sceneName == null) return;

        StartCoroutine(LoadLevelAsyncCoroutine(sceneName, onLoaded, useAttributeCache));
    }

    // Resolves a requested scene name against the scenes that are actually in the build and
    // migrates names that a scene rename left behind in saves or in serialized data.
    // Returns null and logs the reason when nothing can be loaded: SceneManager.LoadSceneAsync
    // answers an unknown name with null + a log line, which used to turn into a
    // NullReferenceException inside the coroutine with the screen already faded to black.
    private static string ResolveSceneName(string requestedName)
    {
        if (string.IsNullOrEmpty(requestedName))
        {
            Debug.LogError("LevelLoader was asked to load an empty scene name, nothing to do.");
            return null;
        }

        if (IsSceneInBuild(requestedName)) return requestedName;

        string renamedTo;
        if (legacySceneNames.TryGetValue(requestedName, out renamedTo) && IsSceneInBuild(renamedTo))
        {
            Debug.LogWarning($"Scene '{requestedName}' was renamed to '{renamedTo}', loading the renamed scene instead.");
            return renamedTo;
        }

        Debug.LogError($"Scene '{requestedName}' cannot be loaded: it is not in Build Settings. Build scenes: {BuildSceneNames()}.");
        return null;
    }

    public static bool IsSceneInBuild(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            if (GetBuildSceneName(i) == sceneName) return true;
        }

        return false;
    }

    private static string BuildSceneNames()
    {
        List<string> names = new List<string>();
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            names.Add(GetBuildSceneName(i));
        }

        return string.Join(", ", names);
    }

    private static string GetBuildSceneName(int buildIndex)
    {
        return Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(buildIndex));
    }

    private PlayerInput GetPlayerInput()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<PlayerInput>() : null;
    }

    private void SavePlayerGASState()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        player?.GetComponent<PlayerGASPersistence>()?.SaveState();
    }

    private void RestorePlayerGASState()
    {
        GameObject newPlayer = GameObject.FindGameObjectWithTag("Player");
        newPlayer?.GetComponent<PlayerGASPersistence>()?.RestoreState();
    }

    private void TeleportPlayer(Vector3 point, Quaternion rotation)
    {
        GameObject newPlayer = GameObject.FindGameObjectWithTag("Player");

        CharacterController cc = newPlayer.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        newPlayer.transform.SetPositionAndRotation(point, rotation);

        if (cc != null) cc.enabled = true;
    }

    IEnumerator LoadLevelAsyncCoroutine(string levelName, System.Action onLoaded, bool useAttributeCache)
    {
        animator.SetTrigger("StartLoading");
        GetPlayerInput()?.DeactivateInput();
        if (useAttributeCache) SavePlayerGASState();
        float MaxVolume = SoundMixerManager.instance.GetMasterVolume();
        SoundMixerManager.instance.ParallelFadeVolume(MixerNames.masterVolume, -80f, MusicFadeTime);

        yield return new WaitForSecondsRealtime(transitionTime);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(levelName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        SoundMixerManager.instance.ParallelFadeVolume(MixerNames.masterVolume, MaxVolume, MusicFadeTime);
        animator.SetTrigger("EndLoading");
        if (useAttributeCache) RestorePlayerGASState();
        onLoaded?.Invoke();
        GetPlayerInput()?.ActivateInput();

        if ("FirstForest" == levelName)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                SaveSystem.SaveGame(player, false);
        }
    }

    IEnumerator LoadLevelAndTeleportAsyncCoroutine(string levelName, Vector3 point, Quaternion rotation, bool useAttributeCache)
    {
        animator.SetTrigger("StartLoading");
        GetPlayerInput()?.DeactivateInput();
        if (useAttributeCache) SavePlayerGASState();
        float MaxVolume = SoundMixerManager.instance.GetMasterVolume();
        SoundMixerManager.instance.ParallelFadeVolume(MixerNames.masterVolume, -80f, MusicFadeTime);

        yield return new WaitForSecondsRealtime(transitionTime);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(levelName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        TeleportPlayer(point, rotation);
        SoundMixerManager.instance.ParallelFadeVolume(MixerNames.masterVolume, MaxVolume, MusicFadeTime);
        animator.SetTrigger("EndLoading");
        if (useAttributeCache) RestorePlayerGASState();
        GetPlayerInput()?.ActivateInput();
    }
}
