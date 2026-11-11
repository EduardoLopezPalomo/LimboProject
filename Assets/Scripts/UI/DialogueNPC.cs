using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class DialogueNPC : MonoBehaviour
{
    [SerializeField] private NPCDialogueData dialogueData;
    [SerializeField] private NPCDialogueData completeQuestData;
    [FormerlySerializedAs("esNPCDeFiesta")]
    [SerializeField] private bool isPartyNpc = false;
    private bool hasGreetedToday = false;

    public string NpcId => dialogueData.NpcId;
    public string NpcName => dialogueData.NpcName;
    public VoiceType NpcVoice => dialogueData.voiceType;
    public NpcPortrait Portrait => dialogueData.portrait;

    private MinimapIcon minimapIcon;

    private void Awake()
    {
        minimapIcon = GetComponentInChildren<MinimapIcon>();
    }

    private void Start()
    {
        RefreshGoalState();
    }

    private void OnEnable()
    {
        if (!isPartyNpc)
            DayNight.OnDayPhaseChanged += OnDayPhaseChanged;

        QuestManager.OnGoalChange += HandleGoalsChanged;
        RefreshGoalState();
    }

    private void OnDisable()
    {
        if (!isPartyNpc)
            DayNight.OnDayPhaseChanged -= OnDayPhaseChanged;

        QuestManager.OnGoalChange -= HandleGoalsChanged;
    }

    private void OnDayPhaseChanged(DayPhase newPhase)
    {
        if (newPhase != DayPhase.Day && newPhase != DayPhase.FirstLight)
        {
            hasGreetedToday = false;
        }
    }

    private void HandleGoalsChanged(HashSet<string> ids)
    {
        RefreshGoalState();
    }

    public List<string> GetDialogueLines()
    {
        List<string> lines = new List<string>();

        if (isPartyNpc)
        {
            lines.Add(dialogueData.partyText);
            return lines;
        }

        DayPhase currentPhaseLocal = DayNight.Instance.CurrentPhase;
        bool isDay = currentPhaseLocal == DayPhase.Day
                  || currentPhaseLocal == DayPhase.FirstLight
                  || currentPhaseLocal == DayPhase.Dawn;

        if (CheckMainQuest())
        {
            lines = dialogueData.QuestDialogue;
            return lines;
        }

        if (CheckCompleteQuest())
        {
            lines = completeQuestData.QuestDialogue;
            return lines;
        }

        if (CheckSecondaryQuest())
        {
            QuestManager.instance.AddSecondaryQuest(dialogueData.GiveSecondaryQuest);
            lines = dialogueData.QuestDialogue;
            return lines;
        }

        if (isDay)
        {
            if (!hasGreetedToday && dialogueData.dayGreeting.Count != 0)
            {
                lines = dialogueData.dayGreeting;
                hasGreetedToday = true;
            }
            else
            {
                lines.Add(dialogueData.dayGreeting[^1]);
            }
        }
        else
        {
            lines.Add(dialogueData.nightText);
        }

        return lines;
    }

    private void RefreshGoalState()
    {
        if (minimapIcon == null) return;

        bool esGoal =
            (QuestManager.IsCurrentGoal(dialogueData.NpcId) && dialogueData.CurrentQuest != null && !dialogueData.CurrentQuest.completed)
            || (completeQuestData != null && QuestManager.IsCurrentGoal(completeQuestData.NpcId) && completeQuestData.CurrentQuest != null && !completeQuestData.CurrentQuest.completed);

        minimapIcon.SetAsGoal(esGoal);
    }

    private bool CheckMainQuest()
    {
        return dialogueData.CurrentQuest != null && dialogueData.QuestDialogue.Count != 0 && QuestManager.instance.GetCurrentQuest() == dialogueData.CurrentQuest.questId;
    }

    private bool CheckCompleteQuest()
    {
        return completeQuestData != null && completeQuestData.QuestDialogue.Count != 0 && QuestManager.instance.GetCurrentQuest() == completeQuestData.CurrentQuest.questId;
    }

    private bool CheckSecondaryQuest()
    {
        return dialogueData.GiveSecondaryQuest != null
               && dialogueData.GiveSecondaryQuest.questType == QuestType.Secondary
               && dialogueData.QuestDialogue.Count != 0
               && !QuestManager.instance.HasSecondaryQuest(dialogueData.GiveSecondaryQuest.questId);
    }
}