using System;
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
    
    private void ShowDescription(string description)
    {
        descriptionText.text = description;
    }
}
