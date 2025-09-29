using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldSpacePromptUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private SoilPlot targetPlot;

    [Header("UI Elements")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Image statusIcon;
    [SerializeField] private GameObject interactionPromptPanel;
    [SerializeField] private TextMeshProUGUI interactionPromptText;

    [Header("State Sprites")]
    [SerializeField] private Sprite tilledSprite; // <- ICON MỚI
    [SerializeField] private Sprite wateredSprite;
    [SerializeField] private Sprite readyToHarvestSprite;

    private Camera mainCamera;

    void Awake()
    {
        if (targetPlot == null) targetPlot = GetComponentInParent<SoilPlot>();
        mainCamera = Camera.main;
        HideInteractionPrompt(); // Luôn ẩn prompt lúc đầu
    }

    void OnEnable()
    {
        targetPlot.OnPlotUpdated += UpdateVisuals;
        UpdateVisuals();
    }

    void OnDisable()
    {
        targetPlot.OnPlotUpdated -= UpdateVisuals;
    }

    void LateUpdate()
    {
        // Xoay Canvas luôn hướng về camera
        worldCanvas.transform.LookAt(worldCanvas.transform.position + mainCamera.transform.rotation * Vector3.forward,
                                     mainCamera.transform.rotation * Vector3.up);
    }

    // Cập nhật cả icon trạng thái và gợi ý tương tác
    private void UpdateVisuals()
    {
        UpdateStatusIcon();
    }

    private void UpdateStatusIcon()
    {
        statusIcon.enabled = true;
        if (targetPlot.IsReadyToHarvest)
        {
            statusIcon.sprite = readyToHarvestSprite;
        }
        else if (targetPlot.IsWatered)
        {
            statusIcon.sprite = wateredSprite;
        }
        else if (targetPlot.IsTilled)
        {
            statusIcon.sprite = tilledSprite; // <- HIỂN THỊ ICON ĐÃ XỚI
        }
        else
        {
            statusIcon.enabled = false; // Đất chưa xới, không hiển thị icon gì cả
        }
    }

    // Các hàm này sẽ được gọi từ PlayerInteractionController
    public void ShowInteractionPrompt(string prompt)
    {
        interactionPromptPanel.SetActive(true);
        interactionPromptText.text = $"[E] {prompt}";
    }

    public void HideInteractionPrompt()
    {
        interactionPromptPanel.SetActive(false);
    }
}