using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemInteractGoal", menuName = "Quests/Goals/Item")]
public class ItemInteractGoal : Goals, IMinimapTarget
{
    public string itemId;
    public string MinimapTargetId => itemId;

    public override void Init()
    {
        QuestItemInteractable.OnItemInteracted -= Interact;
        base.Init();
        QuestItemInteractable.OnItemInteracted += Interact;
    }

    private void Interact(string ItemId)
    {
        if (itemId == ItemId)
        {
            currentAmount++;
            Evaluate();   
        }
    }

    public override void Completed()
    {
        if(completed) return;
        base.Completed();
        QuestItemInteractable.OnItemInteracted -= Interact;
    }
}
