using UnityEngine;
using System.Collections.Generic;

public class DialogueNPC : MonoBehaviour
{
    [SerializeField] private NPCDialogueData dialogueData;
    [SerializeField] private bool esNPCDeFiesta = false;
    private bool haSaludadoHoy = false;

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

        
        DialogoMision dialogoActivo = ObtenerDialogoMisionActivo();

        if (dialogoActivo != null)
        {
            lineas.Add(dialogoActivo.texto);
            if (dialogoActivo.estadoAlCompletar != EstadoMision.Inicio)
                QuestManager.Instance.AvanzarA(dialogoActivo.estadoAlCompletar);
        }
        else if (!string.IsNullOrEmpty(dialogueData.textoTrasCompletarMision) && HayMisionCompletada())
        {
            lineas.Add(dialogueData.textoTrasCompletarMision);
        }

        return lineas; 
    }

    private DialogoMision ObtenerDialogoMisionActivo()
    {
        foreach (var d in dialogueData.dialogosMision)
        {
            if (d.estadoParaMostrar == EstadoMision.Inicio) continue;
            if (!QuestManager.Instance.EstaEnEstado(d.estadoParaMostrar)) continue;
            if (!string.IsNullOrEmpty(d.flagRequerido) && !QuestManager.Instance.TieneFlag(d.flagRequerido)) continue;

            return d;
        }
        return null;
    }

    private bool HayMisionCompletada()
    {
        foreach (var d in dialogueData.dialogosMision)
        {
            if (d.estadoParaMostrar != EstadoMision.Inicio && QuestManager.Instance.EstadoActual > d.estadoParaMostrar)
                return true;
        }
        return false;
    }
}