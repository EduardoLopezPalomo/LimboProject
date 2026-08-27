using System;
using UnityEngine;
using System.Collections.Generic;

public enum EstadoMision
{
    Inicio,                // Antes de empezar
    BuscaSalidaBosque,     // 1. Y ahora, ¿a dónde voy?
    InfoSobreDioses,       // 2. La información es poder
    PreguntaTotem,         // 3. Turismo
    RecuperaPantalones,    // 4. Emergencia intestinal
    SigueMusica,           // 5. ¿Qué es ese barullo?
    RobaComida,            // 6. ¡A ponerse las botas!
    EnfrentaNaius,         // 7. La venganza se sirve en plato caliente
    Completada
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public EstadoMision EstadoActual { get; private set; } = EstadoMision.Inicio;

    public static event Action<EstadoMision> OnEstadoMisionCambiado;

    private HashSet<string> flags = new HashSet<string>(); 

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AvanzarA(EstadoMision nuevoEstado)
    {
        if (nuevoEstado <= EstadoActual) return;
        Debug.Log("Misión avanzada a: " + nuevoEstado);
        EstadoActual = nuevoEstado;
        OnEstadoMisionCambiado?.Invoke(nuevoEstado);
    }

    public bool EstaEnEstado(EstadoMision estado) => EstadoActual == estado;
    public void MarcarFlag(string flag) => flags.Add(flag);
    public bool TieneFlag(string flag) => flags.Contains(flag);
}