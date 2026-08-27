using UnityEngine;
using System.Collections.Generic;

public class DialogueNPC : MonoBehaviour
{
    [SerializeField] private NPCDialogueData dialogueData;
    [SerializeField] private bool esNPCDeFiesta = false;
    private bool haSaludadoHoy = false;

    public string NpcId => dialogueData.NpcId;
    public string NpcName => dialogueData.NpcName; 
    public TipoVoz VozNPC => dialogueData.tipoVoz;
    public Sprite RetratoNPC => dialogueData.retrato;

    private void OnEnable()
    {
        if (!esNPCDeFiesta)
            DayNight.OnDayPhaseChanged += OnDayPhaseChanged;
    }

    private void OnDisable()
    {
        if (!esNPCDeFiesta)
            DayNight.OnDayPhaseChanged -= OnDayPhaseChanged;
    }

    private void OnDayPhaseChanged(DayPhase nuevaFase)
    {
        if (nuevaFase != DayPhase.Day && nuevaFase != DayPhase.FirstLight)
        {
            haSaludadoHoy = false;
        }
    }

    public List<string> ObtenerLineasDialogo()
    {
        List<string> lineas = new List<string>();

        if (esNPCDeFiesta)
        {
            lineas.Add(dialogueData.partyText);
            return lineas;
        }

        DayPhase faseActual = DayNight.Instance.ActualPhase;
        bool esDia = faseActual == DayPhase.Day
                  || faseActual == DayPhase.FirstLight
                  || faseActual == DayPhase.Dawn;

        if (CheckPrincipalQuest())
        {
            lineas = dialogueData.QuestDialogue;
            return lineas;
        }

        if (CheckSecondaryQuest())
        {
            QuestManager.instance.AddSecondaryQuest(dialogueData.GiveSecondaryQuest);
            lineas = dialogueData.QuestDialogue;
            return lineas;
        }

        if (esDia)
        {
            if (!haSaludadoHoy && dialogueData.dayGreeting.Count != 0)
            {
                lineas = dialogueData.dayGreeting;
                haSaludadoHoy = true;
            }
            else
            {
                lineas.Add(dialogueData.dayGreeting[^1]);
            }
        }
        else
        {
            lineas.Add(dialogueData.nightText);
        }

        return lineas; 
    }

    private bool CheckPrincipalQuest()
    {
        return dialogueData.ActualQuest != null && dialogueData.QuestDialogue.Count != 0 && QuestManager.instance.GetActualQuest() == dialogueData.ActualQuest.questId;
    }

    private bool CheckSecondaryQuest()
    {
        return dialogueData.GiveSecondaryQuest != null 
               && dialogueData.GiveSecondaryQuest.questType == QuestType.Secondary
               && dialogueData.QuestDialogue.Count != 0 
               && !QuestManager.instance.HasSecondaryQuest(dialogueData.GiveSecondaryQuest.questId);
    }
}