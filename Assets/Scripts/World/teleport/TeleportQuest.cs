using UnityEngine;
using System.Collections.Generic;

public class TeleportQuest : MonoBehaviour
{
    public List<Quest> quests;

    private MapMarker mapMarker;

    void Awake()
    {
        mapMarker = GetComponent<MapMarker>();
        if (mapMarker == null)
            Debug.LogError($"{name} has a TeleportQuest but no MapMarker on the same GameObject, so this teleport never highlights as a quest goal. Add one (Static / Teleport).", this);
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
        if (quests.Count == 0 || QuestManager.instance == null || mapMarker == null) return;
        foreach (var quest in quests)
        {
            if (quest == null) continue;
            if (QuestManager.instance.GetCurrentQuest() == quest.questId && !quest.completed)
            {
                mapMarker.SetAsGoal(true);
                return;
            }
        }
        mapMarker.SetAsGoal(false);
    }
}
