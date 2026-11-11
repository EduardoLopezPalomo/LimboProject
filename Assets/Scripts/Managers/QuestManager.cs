using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;




public class QuestManager : MonoBehaviour
{
    public static QuestManager instance { get; private set; }

    public static event Action<HashSet<string>> OnGoalChange;

    [FormerlySerializedAs("PrincipalQuests")]
    [SerializeField] private List<Quest> mainQuests;

    private Dictionary<string, Quest> SecondaryQuests;
    private static HashSet<string> currentGoalIds = new HashSet<string>();
    private int currentMainQuestIndex = 0;

    private void OnEnable()
    {
        Quest.OnQuestComplete += OnQuestCompleted;
    }

    private void OnDisable()
    {
        Quest.OnQuestComplete -= OnQuestCompleted;
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
        if (mainQuests.Count > 0)
            ActivateQuest(mainQuests[currentMainQuestIndex], false);
    }

    public void ReStart()
    {
        currentMainQuestIndex = 0;
        Start();
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
        if (questId == mainQuests[currentMainQuestIndex].questId)
        {
            currentMainQuestIndex++;
            if (currentMainQuestIndex >= mainQuests.Count)
            {
                Debug.LogWarning("There is no more quest");
                return;
            }

            ActivateQuest(mainQuests[currentMainQuestIndex], true);
        }
    }

    private void secondaryQuestCompleted(string questId)
    {
        SecondaryQuests.Remove(questId);
    }

    private void ActivateQuest(Quest quest, bool notification)
    {
        quest.InitGoals(notification);
        SetCurrentGoals(mainQuests[currentMainQuestIndex].goals);
    }

    private static void SetCurrentGoals(List<Goals> goals)
    {
        HashSet<string> ids = new HashSet<string>();
        foreach (Goals goal in goals)
        {
            if (goal is IMinimapTarget minimapTarget && !string.IsNullOrEmpty(minimapTarget.MinimapTargetId))
            {
                ids.Add(minimapTarget.MinimapTargetId);
            }
        }
        currentGoalIds = ids;
        OnGoalChange?.Invoke(currentGoalIds);
    }

    public static bool IsCurrentGoal(string id) => !string.IsNullOrEmpty(id) && currentGoalIds.Contains(id);

    public void AddSecondaryQuest(Quest quest)
    {
        if (SecondaryQuests.ContainsKey(quest.questId)) return;

        SecondaryQuests.Add(quest.questId, quest);
    }

    public String GetCurrentQuest()
    {
        return mainQuests[currentMainQuestIndex].questId;
    }

    public bool HasSecondaryQuest(string questId)
    {
        return SecondaryQuests.ContainsKey(questId);
    }

    public int GetQuestNumber()
    {
        return mainQuests.Count;
    }

    public List<Quest> GetActiveQuests()
    {
        List<Quest> activas = new List<Quest>();

        if (currentMainQuestIndex < mainQuests.Count)
            activas.Add(mainQuests[currentMainQuestIndex]);

        activas.AddRange(SecondaryQuests.Values);

        return activas;
    }

    #region SaveAndLoadData
    public int GetQuestIndex()
    {
        return currentMainQuestIndex;
    }

    public void SetQuestIndex(int index)
    {
        while (index > currentMainQuestIndex)
        {
            mainQuests[currentMainQuestIndex].Complete(true);
            currentMainQuestIndex++;
        }

        currentMainQuestIndex = index;
        ActivateQuest(mainQuests[currentMainQuestIndex], false);
    }

    public string[] GetIdOfQuests()
    {
        string[] listId = new string[mainQuests.Count];

        int i = 0;
        foreach (Quest quest in mainQuests)
        {
            listId[i] = quest.questId;
            i++;
        }

        return listId;
    }

    public int[] GetCurrentAmountOfQuest()
    {
        int[] actualAmount = new int[mainQuests[currentMainQuestIndex].goals.Count];

        int i = 0;
        foreach (Goals goal in mainQuests[currentMainQuestIndex].goals)
        {
            actualAmount[i] = goal.currentAmount;
            i++;
        }

        return actualAmount;
    }

    public int[] GetRequiredAmountOfQuest()
    {
        int[] requiredAmount = new int[mainQuests[currentMainQuestIndex].goals.Count];

        int i = 0;
        foreach (Goals goal in mainQuests[currentMainQuestIndex].goals)
        {
            requiredAmount[i] = goal.requiredAmount;
            i++;
        }

        return requiredAmount;
    }

    public bool[] GetCompletedGoalOfQuest()
    {
        bool[] completed = new bool[mainQuests[currentMainQuestIndex].goals.Count];

        int i = 0;
        foreach (Goals goal in mainQuests[currentMainQuestIndex].goals)
        {
            completed[i] = goal.completed;
            i++;
        }

        return completed;
    }


    public void LoadGoalData(int[] currentAmount, int[] requiredAmount, bool[] completed)
    {
        int i = 0;
        foreach (Goals goal in mainQuests[currentMainQuestIndex].goals)
        {
            goal.currentAmount = currentAmount[i];
            goal.requiredAmount = requiredAmount[i];
            goal.completed = completed[i];

            i++;
        }
    }


    #endregion

}