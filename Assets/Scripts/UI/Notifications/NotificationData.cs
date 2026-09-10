using UnityEngine;

public enum NotificationType
{
    GoalCompleted,
    QuestCompleted,
    NewQuest,
    AbilityUnlocked
}

public class NotificationData
{
    public NotificationType notificationType;
    public string title;

    public NotificationData(NotificationType type, string titleText)
    {
        notificationType = type;
        title = titleText;
    }

}
