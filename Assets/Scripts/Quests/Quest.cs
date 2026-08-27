using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum QuestType
{
   Principal,
   Secondary
}

[CreateAssetMenu(fileName = "Quest", menuName = "Quests/Quest")]
public class Quest : ScriptableObject
{
   public static event Action<Quest> OnQuestComplete;
   public string questId;
   public string tittle;
   public string description;
   public QuestType questType = QuestType.Principal;
   public List<Goals> goals;
   public bool completed;
   
   private void OnValidate()
   {
      questId = tittle + Guid.NewGuid().ToString();
   }

   public void InitGoals()
   {
      goals.ForEach(goal => goal.Init());
      completed = false;
      Goals.OnGoalCompleted += CheckGoals;
   }

   public void CheckGoals()
   {
      if (goals.All(goal => goal.completed))
      {
         Complete();
      }
   }

   public bool Complete()
   {
      completed = true;
      OnQuestComplete?.Invoke(this);
      Goals.OnGoalCompleted -= CheckGoals;
      return completed;
   }
}
