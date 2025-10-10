using BrushHit;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[HideMonoScript]
public class GameUIManager : SerializedMonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Title("UI Panels")]
    [Required][SerializeField] private GameObject gameplayHudPanel;
    [Required][SerializeField] private GameObject gameOverPanel;
    [Required][SerializeField] private GameObject levelCompletePanel;

    [Title("Gameplay HUD Elements")]
    [Required][SerializeField] private TextMeshProUGUI scoreText;

    // --- THAY ĐỔI DUY NHẤT LÀ Ở ĐÂY ---
    [Required][SerializeField] private UIBillProgress collectibleProgressBar;
    // ------------------------------------

    [Title("Panel Buttons")]
    [Required][SerializeField] private Button restartButton;
    [Required][SerializeField] private Button backToMenuButton;
    [Required][SerializeField] private string menuSceneName = "MainMenu";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        InitializeUI();
        InitializeListeners();
    }

    private void OnEnable()
    {
        CollectibleManager.OnCollectibleInitialized += UpdateProgress;
        CollectibleManager.OnCollectibleCollected += UpdateProgress;
        ScoreManager.OnScoreChanged += UpdateScoreText;
    }

    private void OnDisable()
    {
        CollectibleManager.OnCollectibleInitialized -= UpdateProgress;
        CollectibleManager.OnCollectibleCollected -= UpdateProgress;
        ScoreManager.OnScoreChanged -= UpdateScoreText;
    }

    private void InitializeUI()
    {
        gameplayHudPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        levelCompletePanel.SetActive(false);
    }

    private void InitializeListeners()
    {
        restartButton.onClick.AddListener(RestartLevel);
        backToMenuButton.onClick.AddListener(LoadMenuScene);
    }

    public void ShowGameOverPanel()
    {
        gameplayHudPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        levelCompletePanel.SetActive(false);
    }

    public void ShowLevelCompletePanel()
    {
        gameplayHudPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        levelCompletePanel.SetActive(true);
    }

    private void UpdateScoreText(int newScore)
    {
        scoreText.text = $"Score: {newScore}";
    }

    private void UpdateProgress(int current, int max)
    {
        if (collectibleProgressBar == null) return;
        // Logic gọi hàm không cần thay đổi vì API nhất quán
        collectibleProgressBar.SetProgress(current, max);
    }

    private void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void LoadMenuScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
}