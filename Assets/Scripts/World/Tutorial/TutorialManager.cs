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

    private bool showTutorial = true;


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

    private void Start()
    {
        RevertToSave();
    }

    public void RevertToSave()
    {
        SettingsData data = SaveSystem.LoadSettingsData();

        if (data == null) return;

        InterfaceData interfaceData = data.interfaceData;

        if (interfaceData == null) return;

        showTutorial = interfaceData.showTutorial;
    }

    public void ShowTutorial(string tutorialID, Sprite image, string text)
    {
        if (!showTutorial) return;

        TutorialUI tutorialUI = tutorialUIs.Find(t => t.tutorialID == tutorialID);

        if (tutorialUI != null)
        {
            tutorialUI.panel.SetActive(true);
            tutorialUI.tutorialImage.sprite = image;
            tutorialUI.tutorialText.text = text;
        }
        else
        {
            Debug.LogWarning("No se encontr� panel para tutorialID: " + tutorialID);
        }
    }


    public void HideTutorial(string tutorialID)
    {
        TutorialUI tutorialUI = tutorialUIs.Find(t => t.tutorialID == tutorialID);
        if (tutorialUI != null)
        {
            tutorialUI.panel.SetActive(false);
        }
    }

}
