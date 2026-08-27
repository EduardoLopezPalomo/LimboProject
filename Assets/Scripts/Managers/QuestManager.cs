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
        SecondaryQuests = new Dictionary<string, Quest>();
        if(PrincipalQuests.Count > 0)
            ActivateQuest(PrincipalQuests[actualPrincipalQuestIndex]);
    }

    private void OnQuestCompleted(Quest quest)
    {
        switch (quest.questType)
        {
            case QuestType.Principal:
                principalQuestCompleted(quest.questId);
                break;
            case QuestType.Secondary:
                secondaryQuestCompleted(quest.questId);
                break;
        }
    }

    private void principalQuestCompleted(string questId)
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

    private void secondaryQuestCompleted(string questId)
    {
        SecondaryQuests.Remove(questId);
    }

    private void ActivateQuest(Quest quest)
    {
        quest.InitGoals();
    }

    public void AddSecondaryQuest(Quest quest)
    {
        if (SecondaryQuests.ContainsKey(quest.questId)) return;
        
        SecondaryQuests.Add(quest.questId, quest);
    }

    public String GetActualQuest()
    {
        return PrincipalQuests[actualPrincipalQuestIndex].questId;
    }

    public bool HasSecondaryQuest(string questId)
    {
        return SecondaryQuests.ContainsKey(questId);
    }
    
    public List<Quest> GetActiveQuests()
    {
        List<Quest> activas = new List<Quest>();

        if (actualPrincipalQuestIndex < PrincipalQuests.Count)
            activas.Add(PrincipalQuests[actualPrincipalQuestIndex]);

        activas.AddRange(SecondaryQuests.Values);

        return activas;
    }
}