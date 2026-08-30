using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class WorldData
{
    public int sceneIndex;

    public int questIndex;
    public string[] questId;
    public int[] currentAmount;
    public int[] requiredAmount;
    public bool[] completed;
    
    public WorldData() { }

    public WorldData(QuestManager questManager)
    {
        sceneIndex = SceneManager.GetActiveScene().buildIndex;
        questIndex = questManager.GetQuestIndex();
        questId = questManager.GetIdOfQuests();
        currentAmount = questManager.GetCurrentAmountOfQuest();
        requiredAmount = questManager.GetRequiredAmountOfQuest();
        completed = questManager.GetCompletedGoalOfQuest();
    }
}
