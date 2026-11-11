using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum VoiceType
{
    Masculine,
    Feminine, 
    Monster
}

[CreateAssetMenu(fileName = "NewNPCDialogue", menuName = "Dialogue/NPC Dialogue Data")]

public class NPCDialogueData : ScriptableObject
{
    public string NpcName;
    [FormerlySerializedAs("tipoVoz")]
    public VoiceType voiceType = VoiceType.Masculine;
    [FormerlySerializedAs("retrato")]
    public NpcPortrait portrait;

    [Header("Dia")]
    [TextArea] public List<string> dayGreeting;   

    [Header("Noche")]
    [TextArea] public string nightText;

    [Header("Fiesta")]
    [TextArea] public string partyText;

    [Header("Quest")] 
    public string NpcId;
    [FormerlySerializedAs("ActualQuest")]
    public Quest CurrentQuest;
    [FormerlySerializedAs("ActualGoal")]
    public Goals CurrentGoal;
    public Quest GiveSecondaryQuest;
    [TextArea] public List<string> QuestDialogue;

    [ContextMenu("Create NpcId")]
    public void CreateNpcId()
    {
        NpcId = NpcName + Guid.NewGuid().ToString();
    }
}
