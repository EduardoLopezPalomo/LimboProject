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

    private MapMarker mapMarker;

    private void Awake()
    {
        mapMarker = GetComponentInChildren<MapMarker>();
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
        if (dialogueData == null)
        {
            Debug.LogError("DialogueNPC on '" + name + "' has no dialogueData, so it cannot build any lines.", this);
            return new List<string>();
        }

        if (isPartyNpc)
        {
            return new List<string> { dialogueData.partyText };
        }

        // T-057: DayNight ships on the GlobalManager prefab, so a scene without one used to throw here,
        // on the first statement of every ordinary conversation. Day is the safer assumption: it costs a
        // greeting variant, not the dialogue. One lookup into a local (T-013/T-023 counting).
        DayNight dayNight = DayNight.Instance;
        DayPhase currentPhaseLocal = dayNight != null ? dayNight.CurrentPhase : DayPhase.Day;
        bool isDay = currentPhaseLocal == DayPhase.Day
                  || currentPhaseLocal == DayPhase.FirstLight
                  || currentPhaseLocal == DayPhase.Dawn;

        // T-057: these three used to hand the caller the ScriptableObject's own List<string>. Nothing
        // mutates it today, but the consumer is UI code holding a queue, and GuardBlockerNPC already
        // copies for exactly this reason - a dialogue in play must not be able to write to the asset.
        if (CheckMainQuest())
        {
            return new List<string>(dialogueData.QuestDialogue);
        }

        if (CheckCompleteQuest())
        {
            return new List<string>(completeQuestData.QuestDialogue);
        }

        if (CheckSecondaryQuest())
        {
            QuestManager.instance.AddSecondaryQuest(dialogueData.GiveSecondaryQuest);
            return new List<string>(dialogueData.QuestDialogue);
        }

        if (isDay)
        {
            // T-057 E3: the else branch indexed dayGreeting[^1] with no length check. 2 of the 74
            // dialogue assets (D_CocaIne, D_Cordelia) ship an empty dayGreeting AND an empty
            // QuestDialogue, so nothing short-circuited first and it threw on the very first
            // conversation, not the second. They only appear in Party, where isPartyNpc returns above,
            // which is the sole reason this was never live.
            if (dialogueData.dayGreeting != null && dialogueData.dayGreeting.Count != 0)
            {
                if (!hasGreetedToday)
                {
                    hasGreetedToday = true;
                    return new List<string>(dialogueData.dayGreeting);
                }

                return new List<string> { dialogueData.dayGreeting[^1] };
            }

            Debug.LogWarning("NPCDialogueData '" + dialogueData.name + "' has an empty dayGreeting, so a daytime " +
                             "conversation has no greeting to show; falling back to nightText.", this);
            return new List<string> { dialogueData.nightText };
        }

        return new List<string> { dialogueData.nightText };
    }

    private void RefreshGoalState()
    {
        if (mapMarker == null) return;

        bool esGoal =
            (QuestManager.IsCurrentGoal(dialogueData.NpcId) && dialogueData.CurrentQuest != null && !dialogueData.CurrentQuest.completed)
            || (completeQuestData != null && QuestManager.IsCurrentGoal(completeQuestData.NpcId) && completeQuestData.CurrentQuest != null && !completeQuestData.CurrentQuest.completed);

        mapMarker.SetAsGoal(esGoal);
    }

    private bool CheckMainQuest()
    {
        return dialogueData.CurrentQuest != null && dialogueData.QuestDialogue.Count != 0 && QuestManager.instance.GetCurrentQuest() == dialogueData.CurrentQuest.questId;
    }

    private bool CheckCompleteQuest()
    {
        // T-057: this dereferenced completeQuestData.CurrentQuest with no guard while RefreshGoalState()
        // two methods above guards the very same expression - same file, opposite answer. An asset wired
        // as the "quest finished" follow-up without a CurrentQuest threw here on every conversation.
        if (completeQuestData == null || completeQuestData.CurrentQuest == null) return false;
        if (completeQuestData.QuestDialogue == null || completeQuestData.QuestDialogue.Count == 0) return false;

        // One lookup, into a local: an inline "if (X.instance != null) X.instance.Foo()" doubles what the
        // T-023 singleton-debt count sees for a call site that got no more coupled (T-013 learned this).
        QuestManager manager = QuestManager.instance;
        if (manager == null) return false;

        return manager.GetCurrentQuest() == completeQuestData.CurrentQuest.questId;
    }

    private bool CheckSecondaryQuest()
    {
        return dialogueData.GiveSecondaryQuest != null
               && dialogueData.GiveSecondaryQuest.questType == QuestType.Secondary
               && dialogueData.QuestDialogue.Count != 0
               && !QuestManager.instance.HasSecondaryQuest(dialogueData.GiveSecondaryQuest.questId);
    }
}