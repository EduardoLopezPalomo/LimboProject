using System;
using System.Collections.Generic;
using UnityEngine;

public enum TipoVoz
{
    Masculina,
    Femenina, 
    Monstruo
}

[CreateAssetMenu(fileName = "NewNPCDialogue", menuName = "Dialogos/NPC Dialogue Data")]

public class NPCDialogueData : ScriptableObject
{
    public string NpcName;
    public TipoVoz tipoVoz = TipoVoz.Masculina;
    public Sprite retrato;

    [Header("Dia")]
    [TextArea] public List<string> dayGreeting;   

    [Header("Noche")]
    [TextArea] public string nightText;

    [Header("Fiesta")]
    [TextArea] public string partyText;

    [Header("Quest")] 
    public string NpcId;
    public Quest ActualQuest;
    public Goals ActualGoal;
    public Quest GiveSecondaryQuest;
    [TextArea] public List<string> QuestDialogue;

    [ContextMenu("Create NpcId")]
    public void CreateNpcId()
    {
        NpcId = NpcName + Guid.NewGuid().ToString();
    }
}
