using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;




public class QuestManager : MonoBehaviour
{
    public static QuestManager instance { get; private set; }
    
    [SerializeField] private List<Quest> PrincipalQuests;
    
    private Dictionary<string, Quest> SecondaryQuests;
    private int actualPrincipalQuestIndex = 0;
    
    private void OnEnable()
    {
        Quest.OnQuestComplete += OnQuestCompleted;
    }
    
    private void OnDisable()
    {
        Quest.OnQuestComplete -=OnQuestCompleted;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ActivateQuest(PrincipalQuests[actualPrincipalQuestIndex]);
    }

    private void OnQuestCompleted(string questId)
    {
        if (questId == PrincipalQuests[actualPrincipalQuestIndex].questId)
        {
            actualPrincipalQuestIndex++;
            if (actualPrincipalQuestIndex >= PrincipalQuests.Count)
            {
                Debug.LogWarning("There is no more quest");
                return;
            }

            ActivateQuest(PrincipalQuests[actualPrincipalQuestIndex]);
        }
    }

    private void ActivateQuest(Quest quest)
    {
        quest.InitGoals();
    }

    public String GetActualQuest()
    {
        return PrincipalQuests[actualPrincipalQuestIndex].questId;
    }
}