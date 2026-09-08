using System;
using UnityEngine;
using System.Collections.Generic;

public class DialogueNPC : MonoBehaviour
{
    [SerializeField] private NPCDialogueData dialogueData;
    [SerializeField] private NPCDialogueData completeQuestData;
    [SerializeField] private bool esNPCDeFiesta = false;
    private bool haSaludadoHoy = false;

    public string NpcId => dialogueData.NpcId;
    public string NpcName => dialogueData.NpcName;
    public TipoVoz VozNPC => dialogueData.tipoVoz;
    public Sprite RetratoNPC => dialogueData.retrato;

    private MinimapIcon minimapIcon;

    private void Awake()
    {
        minimapIcon = GetComponentInChildren<MinimapIcon>();
    }

    private void Start()
    {
        RefreshGoalState();
    }

    private void OnEnable()
    {
        if (!esNPCDeFiesta)
            DayNight.OnDayPhaseChanged += OnDayPhaseChanged;

        QuestManager.OnGoalChange += HandleGoalsChanged;
        RefreshGoalState();
    }

    private void OnDisable()
    {
        if (!esNPCDeFiesta)
            DayNight.OnDayPhaseChanged -= OnDayPhaseChanged;

        QuestManager.OnGoalChange -= HandleGoalsChanged;
    }

    private void OnDayPhaseChanged(DayPhase nuevaFase)
    {
        if (nuevaFase != DayPhase.Day && nuevaFase != DayPhase.FirstLight)
        {
            haSaludadoHoy = false;
        }
    }

    private void HandleGoalsChanged(HashSet<string> ids)
    {
        RefreshGoalState();
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

        if (CheckCompleteQuest())
        {
            lineas = completeQuestData.QuestDialogue;
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

    private void RefreshGoalState()
    {
        if (minimapIcon == null) return;

        bool esGoal =
            (QuestManager.IsCurrentGoal(dialogueData.NpcId) && dialogueData.ActualQuest != null && !dialogueData.ActualQuest.completed)
            || (completeQuestData != null && QuestManager.IsCurrentGoal(completeQuestData.NpcId) && completeQuestData.ActualQuest != null && !completeQuestData.ActualQuest.completed);

        minimapIcon.SetAsGoal(esGoal);
    }

    private bool CheckPrincipalQuest()
    {
        return dialogueData.ActualQuest != null && dialogueData.QuestDialogue.Count != 0 && QuestManager.instance.GetActualQuest() == dialogueData.ActualQuest.questId;
    }

    private bool CheckCompleteQuest()
    {
        return completeQuestData != null && completeQuestData.QuestDialogue.Count != 0 && QuestManager.instance.GetActualQuest() == completeQuestData.ActualQuest.questId;
    }

    private bool CheckSecondaryQuest()
    {
        return dialogueData.GiveSecondaryQuest != null
               && dialogueData.GiveSecondaryQuest.questType == QuestType.Secondary
               && dialogueData.QuestDialogue.Count != 0
               && !QuestManager.instance.HasSecondaryQuest(dialogueData.GiveSecondaryQuest.questId);
    }
}