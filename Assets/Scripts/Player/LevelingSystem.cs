// Path: Assets/Scripts/Player/LevelingSystem.cs
using UnityEngine;
using System;

public class LevelingSystem : MonoBehaviour
{
    public event Action OnLevelUp;
    public event Action<float, float> OnXPChanged;

    [SerializeField] private float baseXpRequirement = 100f;
    [SerializeField] private float xpGrowthFactor = 1.15f;

    public int CurrentLevel { get; private set; } = 1;
    public float CurrentXP { get; private set; } = 0;
    public float XpToNextLevel { get; private set; }

    private PlayerStats playerStats;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        XpToNextLevel = baseXpRequirement;
    }

    private void Start()
    {
        OnXPChanged?.Invoke(CurrentXP, XpToNextLevel);
    }

    public void AddXP(float amount)
    {
        float modifiedXp = amount * playerStats.GetStat(StatType.XPGain);
        CurrentXP += modifiedXp;

        while (CurrentXP >= XpToNextLevel)
        {
            CurrentXP -= XpToNextLevel;
            LevelUp();
        }
        OnXPChanged?.Invoke(CurrentXP, XpToNextLevel);
    }

    private void LevelUp()
    {
        CurrentLevel++;
        XpToNextLevel = baseXpRequirement * Mathf.Pow(xpGrowthFactor, CurrentLevel - 1);
        OnLevelUp?.Invoke();
        Debug.Log($"LEVEL UP! Reached Level {CurrentLevel}!");
    }
}