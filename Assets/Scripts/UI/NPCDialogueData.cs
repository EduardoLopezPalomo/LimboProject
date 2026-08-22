using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCDialogue", menuName = "Dialogos/NPC Dialogue Data")]

public class NPCDialogueData : ScriptableObject
{
    public string NpcName;

    [Header("Dia")]
    [TextArea] public string dayGreeting;   
    [TextArea] public string dayFarewell;   

    [Header("Noche")]
    [TextArea] public string nightText;

    [Header("Fiesta")]
    [TextArea] public string partyText;
}
