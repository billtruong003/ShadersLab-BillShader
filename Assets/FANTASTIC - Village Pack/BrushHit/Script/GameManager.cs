using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace BrushHit
{
    public enum GameState
    {
        Playing,
        Paused,
        LevelComplete,
        GameOver
    }

    [HideMonoScript]
    public class GameManager : SerializedMonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [ShowInInspector, ReadOnly]
        public GameState CurrentState { get; private set; }

        // Kéo các UI Panel tương ứng vào đây (ví dụ: Panel màn hình thua, thắng)
        [Title("UI Panels")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject levelCompletePanel;

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
            StartNewGame();
        }

        private void StartNewGame()
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;

            // Ẩn tất cả các panel khi bắt đầu
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        }

        public void TriggerGameOver()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.GameOver;
            Time.timeScale = 0f; // Dừng game
            if (gameOverPanel != null) gameOverPanel.SetActive(true);

            // Gọi AudioManager để phát âm thanh thua
            AudioManager.Instance?.PlaySound("GameOver");
        }

        public void TriggerLevelComplete()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.LevelComplete;
            Time.timeScale = 0f;
            if (levelCompletePanel != null) levelCompletePanel.SetActive(true);

            AudioManager.Instance?.PlaySound("LevelComplete");
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}