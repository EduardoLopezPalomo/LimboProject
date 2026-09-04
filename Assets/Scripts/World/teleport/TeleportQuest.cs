using UnityEngine;
using System.Collections.Generic;

public class TeleportQuest : MonoBehaviour
{
    public List<Quest> quests;

    private MinimapIcon minimapIcon;

    void Awake()
    {
        minimapIcon = GetComponent<MinimapIcon>();
    }

    private void Start()
    {
        if (quests.Count == 0) return;

        HandleGoalsChanged(null);
    }

    void OnEnable()
    {
        QuestManager.OnGoalChange += HandleGoalsChanged;
        HandleGoalsChanged(null);
    }

    void OnDisable() => QuestManager.OnGoalChange -= HandleGoalsChanged;

    private void HandleGoalsChanged(HashSet<string> ids)
    {
        if (quests.Count == 0 || QuestManager.instance == null) return;
        foreach (var quest in quests)
        {
            if (quest == null) continue;
            if (QuestManager.instance.GetActualQuest() == quest.questId && !quest.completed)
            {
                minimapIcon.SetAsGoal(true);
                return;
            }
        }
        minimapIcon.SetAsGoal(false);
    }
}
