using TMPro;
using UnityEngine;
using System;

public class QuestItemUI : MonoBehaviour
{
    public static event Action<string> OnClickedQuestUI;
    [SerializeField] private TMP_Text tittleText;
    
    private string description;

    public void Setup(Quest quest)
    {
        tittleText.text = quest.tittle;
        description = quest.description;
    }

    public void ClickButton()
    {
        OnClickedQuestUI?.Invoke(description);
    }
}
