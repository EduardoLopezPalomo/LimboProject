using UnityEngine;

public class DisplayManager : MonoBehaviour
{
    public static DisplayManager Instance;

    [HideInInspector] public int maxFPS;
    [HideInInspector] public int[] resolution = new int[2];
    [HideInInspector] public int indexQualityLevel;
    [HideInInspector] public bool vsync;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }

        RevertToSave();
    }

    public void RevertToSave()
    {
        SettingsData data = SaveSystem.LoadSettingsData();

        if (data == null)
        {
            DefaultValues();
            return;
        }

        DisplayData displayData = data.displayData;

        if (displayData == null)
        {
            DefaultValues();
            return;
        }

        maxFPS = displayData.maxFPS;
        resolution = displayData.resolution;
        indexQualityLevel = displayData.indexQualityLevel;
        vsync = displayData.vsync;

        ApplyValues();
    }

    private void ApplyValues()
    {
        Application.targetFrameRate = maxFPS;
        Screen.SetResolution(resolution[0], resolution[1], true);
        QualitySettings.SetQualityLevel(indexQualityLevel);
        QualitySettings.vSyncCount = vsync ? 1 : 0;
    }

    private void DefaultValues()
    {
        maxFPS = Application.targetFrameRate;
        resolution[0] = Screen.currentResolution.width;
        resolution[1] = Screen.currentResolution.height;
        indexQualityLevel = QualitySettings.GetQualityLevel();
        vsync = QualitySettings.vSyncCount == 1;
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

        Screen.SetResolution(width, height, true);
    }

    public void SetQuality(int index)
    {
        indexQualityLevel = index;
        QualitySettings.SetQualityLevel(index);
    }

    public void SetVsync(bool value)
    {
        vsync = value;
        QualitySettings.vSyncCount = value ? 1 : 0;
    }
}
