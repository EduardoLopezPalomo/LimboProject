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

    public int GetQuestNumber()
    {
        return PrincipalQuests.Count;
    }
    
    public List<Quest> GetActiveQuests()
    {
        List<Quest> activas = new List<Quest>();

        if (actualPrincipalQuestIndex < PrincipalQuests.Count)
            activas.Add(PrincipalQuests[actualPrincipalQuestIndex]);

        activas.AddRange(SecondaryQuests.Values);

        return activas;
    }

    #region SaveAndLoadData
    public int GetQuestIndex()
    {
        return actualPrincipalQuestIndex;
    }

    public void SetQuestIndex(int index)
    {
        actualPrincipalQuestIndex = index;
    }
    
    public string[] GetIdOfQuests()
    {
        string[] listId = new string[PrincipalQuests.Count];
        
        int i = 0;
        foreach (Quest quest in PrincipalQuests)
        {
            listId[i] = quest.questId;
            i++;
        }
        
        return listId;
    }

    public int[] GetCurrentAmountOfQuest()
    {
        int[] actualAmount = new int[PrincipalQuests[actualPrincipalQuestIndex].goals.Count];
        
        int i = 0;
        foreach (Goals goal in PrincipalQuests[actualPrincipalQuestIndex].goals)
        {
            actualAmount[i] = goal.currentAmount;
            i++;
        }
        
        return actualAmount;
    }
    
    public int[] GetRequiredAmountOfQuest()
    {
        int[] requiredAmount = new int[PrincipalQuests[actualPrincipalQuestIndex].goals.Count];
        
        int i = 0;
        foreach (Goals goal in PrincipalQuests[actualPrincipalQuestIndex].goals)
        {
            requiredAmount[i] = goal.requiredAmount;
            i++;
        }
        
        return requiredAmount;
    }
    
    public bool[] GetCompletedGoalOfQuest()
    {
        bool[] completed = new bool[PrincipalQuests[actualPrincipalQuestIndex].goals.Count];
        
        int i = 0;
        foreach (Goals goal in PrincipalQuests[actualPrincipalQuestIndex].goals)
        {
            completed[i] = goal.completed;
            i++;
        }
        
        return completed;
    }


    public void LoadGoalData(int[] currentAmount, int[] requiredAmount, bool[] completed)
    {
        int i = 0;
        foreach (Goals goal in PrincipalQuests[actualPrincipalQuestIndex].goals)
        {
            goal.currentAmount = currentAmount[i];
            goal.requiredAmount = requiredAmount[i];
            goal.completed = completed[i];

            i++;
        }
    }
    

    #endregion
    
}