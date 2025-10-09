using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[HideMonoScript]
public class MainMenuController : SerializedMonoBehaviour
{
    private const int LEVELS_PER_PAGE = 25;

    [Title("Cấu hình Dữ liệu")]
    [Required][SerializeField] private LevelDatabase levelDatabase;
    [Required][SerializeField] private string gameSceneName = "GameScene";

    [Title("UI Panels")]
    [Required][SerializeField] private GameObject mainPanel;
    [Required][SerializeField] private GameObject levelSelectPanel;

    [Title("UI Prefabs")]
    [Required][SerializeField] private Button levelButtonPrefab;
    [Required][SerializeField] private GameObject levelPagePrefab;

    [Title("Tham chiếu UI - Main Panel")]
    [Required][SerializeField] private Button playButton;
    [Required][SerializeField] private Button settingsButton;
    [Required][SerializeField] private Button shopButton;

    [Title("Tham chiếu UI - Level Select Panel")]
    [Required][SerializeField] private Transform levelPageContainer;
    [Required][SerializeField] private Button backButton;
    [Required][SerializeField] private Button nextPageButton;
    [Required][SerializeField] private Button previousPageButton;
    [Required][SerializeField] private TextMeshProUGUI pageIndicatorText;

    private readonly List<GameObject> levelPages = new List<GameObject>();
    private int currentPageIndex = 0;
    private int totalPages = 0;

    private void Start()
    {
        InitializeListeners();
        PopulateLevelPages();
        ShowInitialState();
    }

    private void InitializeListeners()
    {
        playButton.onClick.AddListener(ShowLevelSelectPanel);
        backButton.onClick.AddListener(ShowMainPanel);
        nextPageButton.onClick.AddListener(ShowNextPage);
        previousPageButton.onClick.AddListener(ShowPreviousPage);

        // Tạm thời vô hiệu hóa các nút chưa có chức năng
        settingsButton.onClick.AddListener(() => Debug.Log("Settings Button Clicked"));
        shopButton.onClick.AddListener(() => Debug.Log("Shop Button Clicked"));
    }

    private void PopulateLevelPages()
    {
        ClearExistingPages();
        if (levelDatabase.allLevels == null || levelDatabase.allLevels.Count == 0) return;

        int totalLevels = levelDatabase.allLevels.Count;
        totalPages = Mathf.CeilToInt((float)totalLevels / LEVELS_PER_PAGE);

        for (int pageIdx = 0; pageIdx < totalPages; pageIdx++)
        {
            GameObject page = Instantiate(levelPagePrefab, levelPageContainer);
            page.name = $"Page_{pageIdx + 1}";
            levelPages.Add(page);

            int startLevelIndex = pageIdx * LEVELS_PER_PAGE;
            int endLevelIndex = Mathf.Min(startLevelIndex + LEVELS_PER_PAGE, totalLevels);

            for (int levelIdx = startLevelIndex; levelIdx < endLevelIndex; levelIdx++)
            {
                LevelData level = levelDatabase.allLevels[levelIdx];
                Button levelButton = Instantiate(levelButtonPrefab, page.transform);
                levelButton.gameObject.name = $"LevelButton_{levelIdx + 1}";

                var buttonText = levelButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = (levelIdx + 1).ToString();
                }

                levelButton.onClick.AddListener(() => OnLevelSelected(level));
            }
        }
    }

    private void ShowInitialState()
    {
        currentPageIndex = 0;
        ShowPage(currentPageIndex);
        ShowMainPanel();
    }

    private void OnLevelSelected(LevelData selectedLevel)
    {
        if (GameDataPersistence.Instance == null)
        {
            Debug.LogError("GameDataPersistence is not found in the scene.");
            return;
        }

        GameDataPersistence.Instance.SetLevelToLoad(selectedLevel);
        SceneManager.LoadScene(gameSceneName);
    }

    private void ShowPage(int pageIndex)
    {
        currentPageIndex = Mathf.Clamp(pageIndex, 0, totalPages - 1);

        for (int i = 0; i < levelPages.Count; i++)
        {
            levelPages[i].SetActive(i == currentPageIndex);
        }

        UpdateNavigationUI();
    }

    private void UpdateNavigationUI()
    {
        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = $"{currentPageIndex + 1} / {totalPages}";
        }
        previousPageButton.interactable = (currentPageIndex > 0);
        nextPageButton.interactable = (currentPageIndex < totalPages - 1);
    }

    private void ShowNextPage() => ShowPage(currentPageIndex + 1);
    private void ShowPreviousPage() => ShowPage(currentPageIndex - 1);

    private void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        levelSelectPanel.SetActive(false);
    }

    private void ShowLevelSelectPanel()
    {
        mainPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
    }

    private void ClearExistingPages()
    {
        foreach (Transform child in levelPageContainer)
        {
            Destroy(child.gameObject);
        }
        levelPages.Clear();
    }
}