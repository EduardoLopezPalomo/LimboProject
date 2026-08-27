using System;
using UnityEngine;

public class QuestZoneTrigger : MonoBehaviour
{
    public static event Action<string> OnEnterQuestZone;
    public string ZoneId;
    public Quest ActualQuest;

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
