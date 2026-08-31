using UnityEngine;

[System.Serializable]
public class DisplayData
{
    public int maxFPS;
    public int[] resolution;
    public int indexQualityLevel;
    public bool vsync;

    public DisplayData() { }

    public DisplayData(DisplayManager displayManager)
    {
        maxFPS = displayManager.maxFPS;
        resolution = displayManager.resolution;
        indexQualityLevel = displayManager.indexQualityLevel;
        vsync = displayManager.vsync;
    }
}
