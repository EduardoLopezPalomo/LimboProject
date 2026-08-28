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
    
    public WorldData()
    {
        sceneIndex = SceneManager.GetActiveScene().buildIndex;
        questIndex = QuestManager.instance.GetQuestIndex();
        questId = QuestManager.instance.GetIdOfQuests();
        currentAmount = QuestManager.instance.GetCurrentAmountOfQuest();
        requiredAmount = QuestManager.instance.GetRequiredAmountOfQuest();
        completed = QuestManager.instance.GetCompletedGoalOfQuest();
    }
}
