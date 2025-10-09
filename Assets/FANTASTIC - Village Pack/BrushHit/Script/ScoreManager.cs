using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class ScoreManager : SerializedMonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Title("Tham chiếu Giao diện")]
    [Required("Cần tham chiếu đến TextMeshPro để hiển thị điểm.")]
    [SerializeField]
    private TextMeshProUGUI scoreText;

    [Title("Thông tin Điểm")]
    [ShowInInspector]
    [ReadOnly]
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
        UpdateScoreUI();
    }

    public void AddScore(int amount)
    {
        if (amount <= 0) return;

        CurrentScore += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {CurrentScore}";
        }
    }
}