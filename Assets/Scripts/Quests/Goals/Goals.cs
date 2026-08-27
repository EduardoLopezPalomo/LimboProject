using System;
using UnityEngine;


public class Goals : ScriptableObject
{
   public static event Action OnGoalCompleted;
  
   public string tittle;
   public int currentAmount;
   public int requiredAmount;
   public bool completed;
   
   
   public virtual void Init()
   {
      currentAmount = 0;
      completed = false;
   }

   public void Evaluate()
   {
      if (currentAmount >= requiredAmount)
      {
         Completed();
      }
   }

   public virtual void Completed()
   {
      completed = true;
      OnGoalCompleted?.Invoke();
   }


}
