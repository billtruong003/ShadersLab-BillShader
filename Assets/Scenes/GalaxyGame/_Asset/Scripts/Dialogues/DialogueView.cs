using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;

public class DialogueView : MonoBehaviour
{
    [Title("World Space Components")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private RectTransform mainPanel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Title("Content")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image portraitImage;

    [Title("Choices")]
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private Button choiceButtonPrefab;

    [Title("Animation Settings")]
    [SerializeField] private float moveSmoothTime = 0.15f;
    [SerializeField] private float popupDuration = 0.3f;
    [SerializeField] private Vector3 offsetFromTarget = new Vector3(0, 0.5f, 0);

    private List<Button> activeChoices = new List<Button>();
    private Tween typeWriterTween;
    private Transform currentTarget;
    private Vector3 currentVelocity;
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
        if (worldCanvas != null) worldCanvas.worldCamera = mainCam;
        SetActive(false);
    }

    private void LateUpdate()
    {
        if (!mainPanel.gameObject.activeSelf) return;

        HandleBillboard();
        FollowTarget();
    }

    public void SetActive(bool active)
    {
        if (active)
        {
            mainPanel.gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, popupDuration);
            mainPanel.localScale = Vector3.zero;
            mainPanel.DOScale(Vector3.one, popupDuration).SetEase(Ease.OutBack);
        }
        else
        {
            canvasGroup.DOFade(0f, 0.2f).OnComplete(() =>
            {
                mainPanel.gameObject.SetActive(false);
                ClearChoices();
            });
        }
    }

    public void ShowLine(CharacterProfile speaker, string text, float typeSpeed, Action onComplete)
    {
        ClearChoices();
        UpdateTargetPosition(speaker);

        nameText.text = speaker.CharacterName;
        nameText.color = speaker.NameColor;

        if (portraitImage != null)
        {
            portraitImage.sprite = speaker.Portrait;
            portraitImage.enabled = speaker.Portrait != null;
        }

        dialogueText.text = string.Empty;
        typeWriterTween?.Kill();

        // Hiệu ứng "Punch" nhẹ khi bắt đầu câu thoại mới
        mainPanel.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 1);

        typeWriterTween = DOTween.To(() => string.Empty, x => dialogueText.text = x, text, text.Length * typeSpeed)
            .SetEase(Ease.Linear)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void UpdateTargetPosition(CharacterProfile speaker)
    {
        Transform speechPoint = DialogueActorRegistry.GetSpeechPoint(speaker);

        // Nếu không tìm thấy actor trong scene, fallback về vị trí trước camera
        if (speechPoint == null)
        {
            currentTarget = null;
            transform.position = mainCam.transform.position + mainCam.transform.forward * 3f;
        }
        else
        {
            // Nếu đổi người nói, tween vị trí sang người mới
            if (currentTarget != speechPoint)
            {
                currentTarget = speechPoint;
                transform.DOMove(currentTarget.position + offsetFromTarget, 0.3f).SetEase(Ease.OutCubic);
            }
        }
    }

    private void HandleBillboard()
    {
        // Luôn xoay UI về phía Camera nhưng giữ trục thẳng đứng (nếu muốn UI đứng thẳng)
        // Hoặc look rotation trực tiếp để UI nghiêng theo cam
        transform.rotation = mainCam.transform.rotation;
    }

    private void FollowTarget()
    {
        if (currentTarget == null) return;

        // Smooth follow để UI không bị rung khi nhân vật thở/idle
        Vector3 targetPos = currentTarget.position + offsetFromTarget;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, moveSmoothTime);
    }

    public void ShowChoices(List<DialogueChoiceNode.ChoiceOption> options, Action<int> onSelected)
    {
        ClearChoices();
        // Mở rộng panel hoặc hiển thị container choice
        choiceContainer.gameObject.SetActive(true);

        for (int i = 0; i < options.Count; i++)
        {
            int index = i;
            var btn = Instantiate(choiceButtonPrefab, choiceContainer);
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = options[i].Text;

            btn.onClick.AddListener(() => onSelected?.Invoke(index));

            // Animation xuất hiện cho nút
            btn.transform.localScale = Vector3.zero;
            btn.transform.DOScale(Vector3.one, 0.2f).SetDelay(i * 0.1f).SetEase(Ease.OutBack);

            activeChoices.Add(btn);
        }
    }

    public void DisplayFullText(string text)
    {
        typeWriterTween?.Kill();
        dialogueText.text = text;
    }

    private void ClearChoices()
    {
        foreach (var btn in activeChoices)
        {
            if (btn != null)
            {
                btn.transform.DOScale(0f, 0.1f).OnComplete(() => Destroy(btn.gameObject));
            }
        }
        activeChoices.Clear();
    }

    private void OnDisable()
    {
        typeWriterTween?.Kill();
    }
}