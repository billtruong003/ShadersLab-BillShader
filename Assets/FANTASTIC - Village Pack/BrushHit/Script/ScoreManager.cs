using Sirenix.OdinInspector;
using UnityEngine;
using System;

public class ScoreManager : SerializedMonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    public static event Action<int> OnScoreChanged;

    [ShowInInspector, ReadOnly]
    public int CurrentScore { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        CurrentScore = 0;
        OnScoreChanged?.Invoke(CurrentScore);
    }

    public void AddScore(int amount)
    {
        if (amount <= 0) return;

        CurrentScore += amount;
        OnScoreChanged?.Invoke(CurrentScore);
    }
}