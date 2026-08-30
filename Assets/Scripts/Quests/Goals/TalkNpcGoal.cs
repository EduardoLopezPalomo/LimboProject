using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TalkGoal", menuName = "Quests/Goals/Talk")]
public class TalkNpcGoal : Goals, IMinimapTarget
{
    public string npcId;
    public string MinimapTargetId => npcId;
    
    public override void Init()
    {
        DialogueUI.OnEndDialogue -= Talked;
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
        if(completed) return;
        base.Completed();
        DialogueUI.OnEndDialogue -= Talked;
    }
}
