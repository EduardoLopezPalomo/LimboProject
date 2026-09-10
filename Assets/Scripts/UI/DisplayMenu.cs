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

    private static readonly (int width, int height, string label)[] targetResolutions = new[]
    {
        (3840, 2160, "4K (3840x2160)"),
        (2560, 1440, "2K (2560x1440)"),
        (1920, 1080, "1080p (1920x1080)"),
        (1280, 720,  "720p (1280x720)"),
    };

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

            Resolution maxRes = allResolutions
                .OrderByDescending(r => r.width * r.height)
                .FirstOrDefault();

            int maxWidth = maxRes.width > 0 ? maxRes.width : Screen.currentResolution.width;
            int maxHeight = maxRes.height > 0 ? maxRes.height : Screen.currentResolution.height;

            filteredResolutions = targetResolutions
                .Where(t => t.width <= maxWidth && t.height <= maxHeight)
                .Select(t => new Resolution { width = t.width, height = t.height })
                .OrderBy(r => r.width)
                .ToList();
        }

        List<string> options = new List<string>();
        int currentIndex = 0;
        int bestDiff = int.MaxValue;

        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            Resolution res = filteredResolutions[i];
            options.Add(GetLabel(res.width, res.height));

            int diff = Mathf.Abs(res.width - Screen.currentResolution.width) +
                       Mathf.Abs(res.height - Screen.currentResolution.height);

            if (diff < bestDiff)
            {
                bestDiff = diff;
                currentIndex = i;
            }
        }

        resolutions.AddOptions(options);
        resolutions.value = currentIndex;
        resolutions.RefreshShownValue();
    }

    private string GetLabel(int width, int height)
    {
        foreach (var t in targetResolutions)
        {
            if (t.width == width && t.height == height)
                return t.label;
        }
        return $"{width} x {height}";
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
