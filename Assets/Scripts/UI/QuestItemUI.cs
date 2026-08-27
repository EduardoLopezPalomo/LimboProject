using TMPro;
using UnityEngine;
using System;

public class QuestItemUI : MonoBehaviour
{
    public static event Action<Quest> OnClickedQuestUI;
    [SerializeField] private TMP_Text tittleText;
    
    private Quest quest;

    public void Setup(Quest quest)
    {
        tittleText.text = quest.title;
        this.quest = quest;
    }

    public void ClickButton()
    {
        OnClickedQuestUI?.Invoke(quest);
    }
}
