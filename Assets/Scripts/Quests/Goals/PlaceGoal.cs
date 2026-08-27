using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlaceGoal", menuName = "Quests/Goals/Place")]
public class PlaceGoal : Goals
{
    public string ZoneId;
    public override void Init()
    {
        base.Init();
        QuestZoneTrigger.OnEnterQuestZone += ArrivePlace;
    }

    private void ArrivePlace(string zoneId)
    {
        if (ZoneId == zoneId)
        {
            currentAmount++;
            Evaluate();   
        }
    }
    
    public override void Completed()
    {
        base.Completed();
        QuestZoneTrigger.OnEnterQuestZone -= ArrivePlace;
    }
   
}
