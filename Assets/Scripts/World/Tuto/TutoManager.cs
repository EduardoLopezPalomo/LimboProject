using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [System.Serializable]
    public class TutorialUI
    {
        public string tutorialID;       // ID o nombre para identificar el tutorial
        public GameObject panel;        // panel (UI) que quieres mostrar
        public Image tutorialImage;     // referencia a la imagen dentro del panel
        public TMP_Text tutorialText;   // referencia al texto dentro del panel
    }

    public List<TutorialUI> tutorialUIs;

    private TutorialUI currentTutorialUI;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Oculta todos los paneles al inicio
        foreach (var t in tutorialUIs)
        {
            t.panel.SetActive(false);
        }
    }

    // Método para mostrar tutorial por ID
    public void ShowTutorial(string tutorialID, Sprite image, string text)
    {
        // Ocultar el panel actual si hay
        if (currentTutorialUI != null)
            currentTutorialUI.panel.SetActive(false);

        // Buscar el tutorialUI con ese ID
        currentTutorialUI = tutorialUIs.Find(t => t.tutorialID == tutorialID);

        if (currentTutorialUI != null)
        {
            currentTutorialUI.panel.SetActive(true);
            currentTutorialUI.tutorialImage.sprite = image;
            currentTutorialUI.tutorialText.text = text;
        }
        else
        {
            Debug.LogWarning("No se encontró panel para tutorialID: " + tutorialID);
        }
    }

    public void HideTutorial()
    {
        if (currentTutorialUI != null)
        {
            currentTutorialUI.panel.SetActive(false);
            currentTutorialUI = null;
        }
    }
}
