using System;

public static class GameEvents
{
    public static event Action<bool> OnGameStateChanged;

    public static void SetGameControlLock(bool isLocked)
    {
        OnGameStateChanged?.Invoke(isLocked);
    }
}