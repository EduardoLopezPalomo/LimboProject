using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TalkGoal", menuName = "Quests/Goals/Talk")]
public class TalkNpcGoal : Goals
{
    public string npcId;
    
    public override void Init()
    {
        base.Init();
        DialogueUI.OnEndDialogue += Talked;
    }

    private void Talked(string NpcId)
    {
        if (NpcId == npcId)
        {
            currentAmount++;
            Evaluate();   
        }
    }

    public override void Completed()
    {
        base.Completed();
        DialogueUI.OnEndDialogue -= Talked;
    }
}
