using System;

public static class GameplayWarning
{
    public static event Action<string> OnWarning;

    public static void Show(string message)
    {
        OnWarning?.Invoke(message);
    }
}