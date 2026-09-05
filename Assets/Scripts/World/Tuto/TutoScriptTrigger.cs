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
            }
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == player)
        {
            foreach (var content in tutorialContents)
            {
                tutorialManager.HideTutorial(content.tutorialID);
            }
        }
    }

}

