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
   public string title;
   public string description;
   public QuestType questType = QuestType.Principal;
   public List<Goals> goals;
   public bool completed;
   
   public void CreateId()
   {
      questId = title + Guid.NewGuid().ToString();
   }

   public void InitGoals()
   {
      Goals.OnGoalCompleted -= CheckGoals;
      goals.ForEach(goal => goal.Init());
      completed = false;
      
      if (NotificationManager.Instance != null)
      {
         NotificationManager.Instance.Enqueue(new NotificationData(NotificationType.NewQuest, title));  
      }
      
      Goals.OnGoalCompleted += CheckGoals;
   }

   public void CheckGoals()
   {
      if (goals.All(goal => goal.completed))
      {
         Complete();
      }
   }

   public bool Complete(bool fromLoad = false)
   {
      if (completed) return true;
      
      if (!fromLoad)
      {
         NotificationManager.Instance.Enqueue(new NotificationData(NotificationType.QuestCompleted, title));
         OnQuestComplete?.Invoke(this);
      }
      completed = true;
      Goals.OnGoalCompleted -= CheckGoals;
      return completed;
   }
}
