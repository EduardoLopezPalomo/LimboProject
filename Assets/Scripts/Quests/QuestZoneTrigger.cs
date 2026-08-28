using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestZoneTrigger : MonoBehaviour
{
    public static event Action<string> OnEnterQuestZone;
    public string ZoneId;
    public Quest ActualQuest;
    
    private MinimapIcon minimapIcon;

    void Awake()
    {
        minimapIcon = GetComponent<MinimapIcon>();
        minimapIcon.enabled = false;
    }

    void OnEnable()
    {
        QuestManager.OnGoalChange += HandleGoalsChanged;
        HandleGoalsChanged(null);
    }

    void OnDisable() =>  QuestManager.OnGoalChange -= HandleGoalsChanged;

    private void HandleGoalsChanged(HashSet<string> ids)
    {
        bool esObjetivo = QuestManager.IsCurrentGoal(ZoneId);
        minimapIcon.enabled = esObjetivo;
        if (esObjetivo) minimapIcon.SetAsGoal(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") &&  ActualQuest != null && QuestManager.instance.GetActualQuest() == ActualQuest.questId)
        {
            OnEnterQuestZone?.Invoke(ZoneId);
            gameObject.SetActive(false);
        }
    }
    
    [ContextMenu("Create Id")]
    public void CreateId()
    {
        ZoneId = "Zone" + Guid.NewGuid().ToString();
    }
}
