using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;


public class TutorialTrigger : MonoBehaviour
{
    public List<TutorialContent> tutorialContents;
    public TutorialManager tutorialManager;
    public GameObject player;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player && InterfaceManager.Instance.showTutorial)
        {
            foreach (var content in tutorialContents)
            {
                tutorialManager.ShowTutorial(content.tutorialID, content.tutorialSprite, content.tutorialDescription);
                // Aquí podrías agregar lógica para mostrar uno tras otro o simultáneamente si el tutorialManager lo permite
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == player)
        {
            tutorialManager.HideTutorial();
        }
    }
}

