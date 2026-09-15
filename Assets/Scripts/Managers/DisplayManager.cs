using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class DisplayManager : MonoBehaviour
{
    public static DisplayManager Instance;
    
    public int defaultMaxFPS = 60;
    public int defaultQualityLevel = 3;

    [HideInInspector] public int maxFPS;
    [HideInInspector] public int[] resolution = new int[2];
    [HideInInspector] public int indexQualityLevel;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }
        
        int nativeWidth = Display.main.systemWidth;
        int nativeHeight = Display.main.systemHeight;
        
        /* Debug.Log($"[DisplayManager] ANTES - currentResolution: {nativeWidth}x{nativeHeight}, " +
                  $"Screen.width/height: {Screen.width}x{Screen.height}, " +
                  $"fullScreenMode: {Screen.fullScreenMode}");
        */

        bool alreadyCorrectMode = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
        bool alreadyCorrectRes = Screen.width == nativeWidth && Screen.height == nativeHeight;

        if (!alreadyCorrectMode || !alreadyCorrectRes)
        {
            Screen.SetResolution(nativeWidth, nativeHeight, FullScreenMode.FullScreenWindow);
            //Debug.Log("[DisplayManager] Se forzó cambio de modo/resolución");
        }
        
        RevertToSave();
        /*
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        Debug.Log($"[DisplayManager] DESPUES - resolution guardada: {resolution[0]}x{resolution[1]}, " +
                  $"renderScale aplicado: {(urpAsset != null ? urpAsset.renderScale.ToString() : "null asset")}, " +
                  $"Screen.currentResolution ahora: {Screen.currentResolution.width}x{Screen.currentResolution.height}");
        */
    }
    
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void RevertToSave()
    {
        SettingsData data = SaveSystem.LoadSettingsData();

        if (data == null || data.displayData == null)
        {
            DefaultValues();
            ApplyValues();
            return;
        }

        DisplayData displayData = data.displayData;

        maxFPS = displayData.maxFPS > 0 ? displayData.maxFPS : defaultMaxFPS;

        if (displayData.resolution != null && displayData.resolution.Length == 2
                                           && displayData.resolution[0] > 0 && displayData.resolution[1] > 0)
        {
            resolution = displayData.resolution;
        }
        else
        {
            resolution = GetMaxSupportedResolution();
        }

        int qualityCount = QualitySettings.names.Length;
        indexQualityLevel = (displayData.indexQualityLevel >= 0 && displayData.indexQualityLevel < qualityCount)
            ? displayData.indexQualityLevel
            : defaultQualityLevel;

        ApplyValues();
    }

    private void ApplyValues()
    {
        Application.targetFrameRate = maxFPS;
        ApplyRenderScale(resolution[0], resolution[1]);
        QualitySettings.SetQualityLevel(indexQualityLevel);
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyRenderScale(resolution[0], resolution[1]);
    }
    
    private void ApplyRenderScale(int targetWidth, int targetHeight)
    {
        Resolution nativeRes = Screen.currentResolution;

        float scaleByWidth = (float)targetWidth / nativeRes.width;
        float scaleByHeight = (float)targetHeight / nativeRes.height;
        float renderScale = Mathf.Min(scaleByWidth, scaleByHeight);

        renderScale = Mathf.Clamp(renderScale, 0.25f, 2f); 
        
        //Debug.Log($"[ApplyRenderScale] target: {targetWidth}x{targetHeight}, nativeRes usado: {nativeRes.width}x{nativeRes.height}, renderScale resultante: {renderScale}");
        
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            urpAsset.renderScale = renderScale;
        }
        else
        {
            //Debug.LogWarning("[ApplyRenderScale] urpAsset es null — no se aplicó renderScale");
        }
    }

    private void DefaultValues()
    {
        maxFPS = defaultMaxFPS > 0 ? defaultMaxFPS : Application.targetFrameRate;

        resolution = GetMaxSupportedResolution();

        int qualityCount = QualitySettings.names.Length;
        indexQualityLevel = (defaultQualityLevel >= 0 && defaultQualityLevel < qualityCount)
            ? defaultQualityLevel
            : QualitySettings.GetQualityLevel();
        
    }
    
    private int[] GetMaxSupportedResolution()
    {
        Resolution[] allResolutions = Screen.resolutions;

        if (allResolutions != null && allResolutions.Length > 0)
        {
            Resolution maxRes = allResolutions
                .OrderByDescending(r => r.width * r.height)
                .First();

            return new int[] { maxRes.width, maxRes.height };
        }
        
        return new int[] { Screen.currentResolution.width, Screen.currentResolution.height };
    }

    public void SetMaxFPS(int value)
    {
        maxFPS = value;
        Application.targetFrameRate = maxFPS;
    }

    public void SetResolution(int width, int height)
    {
        resolution[0] = width;
        resolution[1] = height;

        ApplyRenderScale(width, height);
    }

    public void SetQuality(int index)
    {
        indexQualityLevel = index;
        QualitySettings.SetQualityLevel(index);
    }
}
