using UnityEngine;

public class TriggerPrincipalQuest : MonoBehaviour
{
    private QuestZoneTrigger questTrigger;


    void Awake()
    {
        questTrigger = GetComponent<QuestZoneTrigger>();
    }
    void OnEnable()
    {
        DialogueUI.OnEndDialogue += DialogueEnd;
    }

    void OnDisable()
    {
        DialogueUI.OnEndDialogue -= DialogueEnd;
    }

    private void DialogueEnd(string id)
    {
        if (questTrigger.ActivateEndDialogue)
        {
            questTrigger.AlertQuest();
            this.enabled = false;
        }
    }
}
