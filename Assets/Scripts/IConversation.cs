using System;

public interface IConversation 
{
    public static event Action<DialogueInitData> OnDialogueInit;
    
    public static void RaiseDialogueInit(DialogueInitData data)
    {
        OnDialogueInit?.Invoke(data);
    }
}
