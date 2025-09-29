using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    public event Action OnDayAdvanced;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void AdvanceDay()
    {
        Debug.Log("A new day has begun!");
        OnDayAdvanced?.Invoke();
    }

    // --- Phương thức để test ---
    [ContextMenu("Advance Day")]
    private void TestAdvanceDay()
    {
        AdvanceDay();
    }
}