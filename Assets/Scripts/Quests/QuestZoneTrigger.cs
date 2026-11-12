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

    private MapMarker mapMarker;
    private BoxCollider boxCollider;

    void Awake()
    {
        mapMarker = GetComponent<MapMarker>();
        if (mapMarker == null)
            Debug.LogError($"{name} has a QuestZoneTrigger but no MapMarker on the same GameObject, so this quest zone never appears on the map. Add one (Static / MissionPoint).", this);
        else
            mapMarker.enabled = false;
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
        bool showGoal = isObjective && !CurrentGoal.completed;

        if (mapMarker != null)
        {
            mapMarker.enabled = isObjective;
            mapMarker.SetAsGoal(showGoal);
        }

        boxCollider.enabled = showGoal;
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
