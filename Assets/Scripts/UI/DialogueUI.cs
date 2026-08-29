using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[Serializable]
public struct DialogueInitData
{
    public List<string> lineas;
    public string npcId;
    public string nombre;
    public TipoVoz voz;
    public Sprite retrato;
    public PlayerInput input;
}

public class DialogueUI : MonoBehaviour
{
    public static event System.Action<string> OnEndDialogue;
    
    [SerializeField] private GameObject panelDialogo;
    [SerializeField] private TMP_Text textoDialogo;
    [SerializeField] private TMP_Text nombreNPC;
    [SerializeField] private Image retratoNPC;

    [Header("Efecto de escritura")]
    [SerializeField] private float velocidadEscritura = 0.03f; 
    [SerializeField] private SFXDefinition sfxHablaMasculina;
    [SerializeField] private SFXDefinition sfxHablaFemenina;
    [SerializeField] private SFXDefinition sfxHablaMonstruo;

    private string actualNpcId;
    private TipoVoz vozActual;
    private Queue<string> colaLineas = new Queue<string>();
    private PlayerInput playerInputActual;
    private Coroutine escrituraCoroutine;
    private bool escribiendo = false;
    private string lineaActual = "";
    private AudioClip LastClip;
    private AudioSource LastAudioSource;

    private bool InDialogue = false;

    private void OnEnable()
    {
        IConversation.OnDialogueInit += DialogueInit;
    }

    private void OnDisable()
    {
        IConversation.OnDialogueInit -= DialogueInit;
    }

    private void Update()
    {
        if (!InDialogue || playerInputActual == null) return;

        InputAction interactAction = playerInputActual.currentActionMap.FindAction("Interact");
        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            if (escribiendo)
            {
                CompletarLineaInstantaneamente();
            }
            else
            {
                MostrarSiguienteLinea();
            }
        }
    }

    private void DialogueInit(DialogueInitData data)
    {
        InDialogue = true;
        MostrarDialogo(data.lineas, data.npcId, data.nombre, data.voz, data.retrato, data.input);
    }

    private void MostrarDialogo(List<string> lineas , string npcId, string nombre, TipoVoz voz, Sprite retrato, PlayerInput input = null)
    {

        colaLineas = new Queue<string>(lineas);
        playerInputActual = input;
        nombreNPC.text = nombre;
        vozActual = voz;
        actualNpcId = npcId;
        if (retratoNPC != null)
            retratoNPC.sprite = retrato;

        panelDialogo.SetActive(true);
        MostrarSiguienteLinea();
    }

    public void MostrarSiguienteLinea()
    {
        if (colaLineas.Count == 0)
        {
            CerrarDialogo();
            return;
        }

        lineaActual = colaLineas.Dequeue();

        if (escrituraCoroutine != null)
            StopCoroutine(escrituraCoroutine);

        escrituraCoroutine = StartCoroutine(EscribirTexto(lineaActual));
    }

    private IEnumerator EscribirTexto(string linea)
    {
        escribiendo = true;
        textoDialogo.text = "";
        
        

        SFXDefinition clipVoz = ObtenerClipSegunVoz();
        if (clipVoz != null)
        {
            LastAudioSource = SoundFXManager.instance.PlaySoundRandomPitch(clipVoz.sfx, transform, clipVoz.volume , clipVoz.minPitch, clipVoz.maxPitch);
            LastClip = clipVoz.sfx;
        }

        foreach (char letra in linea)
        {
            textoDialogo.text += letra;
            yield return new WaitForSeconds(velocidadEscritura);
        }

        escribiendo = false;
    }

    private SFXDefinition ObtenerClipSegunVoz()
    {
        switch (vozActual)
        {
            case TipoVoz.Masculina: return sfxHablaMasculina;
            case TipoVoz.Femenina: return sfxHablaFemenina;
            case TipoVoz.Monstruo: return sfxHablaMonstruo;
            default: return sfxHablaMasculina;
        }
    }

    private void CompletarLineaInstantaneamente()
    {
        if (escrituraCoroutine != null)
            StopCoroutine(escrituraCoroutine);
        
        if(LastClip != null && LastAudioSource != null)
        {
            SoundFXManager.instance.StopSound(LastClip, LastAudioSource);
        }

        textoDialogo.text = lineaActual;
        escribiendo = false;
    }

    private void CerrarDialogo()
    {
        panelDialogo.SetActive(false);
        
        if(LastClip != null && LastAudioSource != null)
        {
            SoundFXManager.instance.StopSound(LastClip, LastAudioSource);
        }
        
        LastClip = null;
        LastAudioSource = null;
        InDialogue = false;
        if (playerInputActual != null)
            playerInputActual.SwitchCurrentActionMap("Player");
        OnEndDialogue?.Invoke(actualNpcId);
    }
}