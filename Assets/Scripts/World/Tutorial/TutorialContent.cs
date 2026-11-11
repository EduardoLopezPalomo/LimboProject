using UnityEngine;

[System.Serializable]
public class TutorialContent
{
    public string tutorialID;
    public Sprite tutorialSprite;
    [TextArea(3, 10)]
    public string tutorialDescription;
}
