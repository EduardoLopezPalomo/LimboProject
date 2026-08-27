using System;
using System.Text;
using TMPro;
using UnityEngine;

public class DescriptionQuestUI : MonoBehaviour
{
    [SerializeField] private TMP_Text descriptionText;
    private void OnEnable()
    {
        QuestItemUI.OnClickedQuestUI += ShowDescription;
    }

    private void OnDisable()
    {
        QuestItemUI.OnClickedQuestUI -= ShowDescription;
        descriptionText.text = "";
    }
    
    private void ShowDescription(Quest quest)
    {
        StringBuilder text = new StringBuilder();
        
        text.AppendLine(quest.description);
        
        foreach (Goals goal in quest.goals)
        {
            text.AppendLine($"{goal.title} [{goal.currentAmount}/{goal.requiredAmount}]");
        }

        descriptionText.text = text.ToString();
    }
}
