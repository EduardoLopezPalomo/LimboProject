using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class QuestZoneTrigger : MonoBehaviour
{
    public static event Action<string> OnEnterQuestZone;
    public string ZoneId;
    [FormerlySerializedAs("ActualQuest")]
    public Quest CurrentQuest;
    [FormerlySerializedAs("ActualGoal")]
    public Goals CurrentGoal;

    public bool ActivateEndDialogue = true;

    private MinimapIcon minimapIcon;
    private BoxCollider boxCollider;

    void Awake()
    {
        minimapIcon = GetComponent<MinimapIcon>();
        minimapIcon.enabled = false;
        boxCollider = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        if (CurrentGoal == null) return;

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
        bool isObjective = QuestManager.IsCurrentGoal(ZoneId);
        minimapIcon.enabled = isObjective;
        if (isObjective && !CurrentGoal.completed)
        {
            minimapIcon.SetAsGoal(true);
            boxCollider.enabled = true;
        }
        else
        {
            minimapIcon.SetAsGoal(false);
            boxCollider.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ActivateEndDialogue) return;

        if (other.gameObject.CompareTag("Player"))
        {
            AlertQuest();
        }
    }

    public void AlertQuest()
    {
        if (CurrentQuest != null && QuestManager.instance.GetCurrentQuest() == CurrentQuest.questId && !CurrentGoal.completed)
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
