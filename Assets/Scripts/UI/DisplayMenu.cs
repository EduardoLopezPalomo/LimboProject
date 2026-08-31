using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine.Rendering;
using System.Linq;

public class DisplayMenu : MonoBehaviour
{
    public TMP_Dropdown maxFPS;
    public TMP_Dropdown resolutions;
    public TMP_Dropdown qualityLevel;
    public Toggle vsync;

    private List<Resolution> filteredResolutions = new List<Resolution>();

    void OnEnable()
    {
        SetUpMaxFPS();
        SetUpQualityOptions();
        SetUpResolutions();
        vsync.isOn = DisplayManager.Instance.vsync;
    }

    void SetUpMaxFPS()
    {
        int value = DisplayManager.Instance.maxFPS;
        int index = 0;
        switch (value)
        {
            case 30:
                index = 0;
                break;
            case 60:
                index = 1;
                break;
            case 90:
                index = 2;
                break;
            case 120:
                index = 3;
                break;
        }

        maxFPS.value = index;
        maxFPS.RefreshShownValue();
    }

    void SetUpQualityOptions()
    {
        qualityLevel.ClearOptions();
        List<string> qualityNames = new List<string>(QualitySettings.names);

        qualityLevel.AddOptions(qualityNames);

        qualityLevel.value = QualitySettings.GetQualityLevel();
        qualityLevel.RefreshShownValue();
    }

    void SetUpResolutions()
    {
        resolutions.ClearOptions();
        if (filteredResolutions.Count == 0)
        {
            Resolution[] allResolutions = Screen.resolutions;

            filteredResolutions = allResolutions
                .GroupBy(r => new { r.width, r.height })
                .Select(g => g.First())
                .OrderBy(r => r.width)
                .ToList();
        }

        List<string> options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            Resolution res = filteredResolutions[i];
            options.Add($"{res.width} X {res.height}");

            if (res.width == Screen.currentResolution.width && res.height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        resolutions.AddOptions(options);
        resolutions.value = currentIndex;
        resolutions.RefreshShownValue();
    }

    public void OnQualityChange(int index)
    {
        DisplayManager.Instance.SetQuality(index);
    }

    public void OnMaxFpsChange(int index)
    {
        int fpsValue = 60;
        switch (index)
        {
            case 0:
                fpsValue = 30;
                break;
            case 1:
                fpsValue = 60;
                break;
            case 2:
                fpsValue = 90;
                break;
            case 3:
                fpsValue = 120;
                break;
        }

        DisplayManager.Instance.SetMaxFPS(fpsValue);
    }

    public void OnResolutionsChange(int index)
    {
        Resolution res = filteredResolutions[index];
        DisplayManager.Instance.SetResolution(res.width, res.height);
    }

    public void OnVsyncChange(bool value)
    {
        DisplayManager.Instance.SetVsync(value);
    }
}
