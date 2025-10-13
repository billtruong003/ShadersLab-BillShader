using Sirenix.OdinInspector;
using UnityEngine;

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
        }

        public void TriggerGameOver()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.GameOver;
            // Time.timeScale = 0f;
            GameUIManager.Instance?.ShowGameOverPanel();
            AudioManager.Instance?.PlaySound("GameOver");
        }

        public void TriggerLevelComplete()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.LevelComplete;
            // Time.timeScale = 0f;
            GameUIManager.Instance?.ShowLevelCompletePanel();
            AudioManager.Instance?.PlaySound("LevelComplete");
        }
    }
}