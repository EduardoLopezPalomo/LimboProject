using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class WorldData
{
    public string sceneName;

    public int questIndex;
    public string[] questId;
    public int[] currentAmount;
    public int[] requiredAmount;
    public bool[] completed;

    public WorldData() { }

    public WorldData(QuestManager questManager)
    {
        sceneName = SceneManager.GetActiveScene().name;
        questIndex = questManager.GetQuestIndex();
        questId = questManager.GetIdOfQuests();
        currentAmount = questManager.GetCurrentAmountOfQuest();
        requiredAmount = questManager.GetRequiredAmountOfQuest();
        completed = questManager.GetCompletedGoalOfQuest();
    }
}
