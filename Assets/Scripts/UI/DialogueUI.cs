using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject panelDialogo;
    [SerializeField] private TMP_Text textoDialogo;
    [SerializeField] private TMP_Text nombreNPC;
    [SerializeField] private Image retratoNPC;

    [Header("Efecto de escritura")]
    [SerializeField] private float velocidadEscritura = 0.03f; 
    [SerializeField] private SFXDefinition sfxHablaMasculina;
    [SerializeField] private SFXDefinition sfxHablaFemenina;
    [SerializeField] private SFXDefinition sfxHablaMonstruo;

    private TipoVoz vozActual;
    private Queue<string> colaLineas = new Queue<string>();
    private PlayerInput playerInputActual;
    private Coroutine escrituraCoroutine;
    private bool escribiendo = false;
    private string lineaActual = "";

    public bool EnDialogo => panelDialogo.activeSelf;

    private void Update()
    {
        if (!EnDialogo || playerInputActual == null) return;

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

    public void MostrarDialogo(List<string> lineas, string nombre, TipoVoz voz, Sprite retrato, PlayerInput input = null)
    {

        colaLineas = new Queue<string>(lineas);
        playerInputActual = input;
        nombreNPC.text = nombre;
        vozActual = voz;

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
            SoundFXManager.instance.PlaySoundRandomPitch(clipVoz.sfx, transform, clipVoz.volume , clipVoz.minPitch, clipVoz.maxPitch);    
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

        if (audioSource != null)
            audioSource.Stop();

        textoDialogo.text = lineaActual;
        escribiendo = false;
    }

    private void CerrarDialogo()
    {
        panelDialogo.SetActive(false);

        if (audioSource != null)
            audioSource.Stop();

        if (playerInputActual != null)
            playerInputActual.SwitchCurrentActionMap("Player");
    }
}