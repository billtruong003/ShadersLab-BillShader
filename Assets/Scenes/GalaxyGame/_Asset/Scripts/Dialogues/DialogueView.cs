using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;

public class DialogueView : MonoBehaviour
{
    [Title("Layout Containers")]
    [SerializeField] private GameObject mainContainer;

    [Title("Portraits")]
    [SerializeField] private Image playerPortrait; // Ảnh bên Trái (Player)
    [SerializeField] private Image npcPortrait;    // Ảnh bên Phải (NPC)

    [Title("Visual Settings")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.4f, 0.4f, 0.4f, 1f); // Màu xám tối
    [SerializeField] private float fadeDuration = 0.3f;

    [Title("Text Elements")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Title("Choices")]
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private Button choiceButtonPrefab;

    private List<Button> activeChoices = new List<Button>();
    private Tween typeWriterTween;
    private Tween playerColorTween;
    private Tween npcColorTween;

    public void SetActive(bool active)
    {
        mainContainer.SetActive(active);
        if (!active) ClearChoices();
    }

    public void ShowLine(CharacterProfile speaker, string text, float typeSpeed, Action onComplete)
    {
        ClearChoices();

        // 1. Setup Data hiển thị
        nameText.text = speaker.CharacterName;
        nameText.color = speaker.NameColor;

        // 2. Cập nhật ảnh và hiệu ứng sáng/tối
        UpdatePortraitVisuals(speaker);

        // 3. Chạy Typewriter
        dialogueText.text = string.Empty;
        typeWriterTween?.Kill();
        typeWriterTween = DOTween.To(() => string.Empty, x => dialogueText.text = x, text, text.Length * typeSpeed)
            .SetEase(Ease.Linear)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void UpdatePortraitVisuals(CharacterProfile speaker)
    {
        // Xác định ai đang nói dựa trên CharacterType
        bool isPlayerSpeaking = speaker.Type == CharacterType.Player;

        // Cập nhật Sprite (Chỉ cập nhật sprite cho bên đang nói để tránh đổi ảnh bên kia nếu không cần thiết)
        // Hoặc bạn có thể set cứng ảnh Player lúc Init nếu Player không thay đổi biểu cảm
        if (isPlayerSpeaking)
            playerPortrait.sprite = speaker.Portrait;
        else
            npcPortrait.sprite = speaker.Portrait;

        // Đảm bảo ảnh luôn hiển thị (phòng trường hợp bị tắt)
        playerPortrait.enabled = playerPortrait.sprite != null;
        npcPortrait.enabled = npcPortrait.sprite != null;

        // Xử lý Tween màu (Highlight người nói, Dim người nghe)
        HighlightSpeaker(isPlayerSpeaking);
    }

    private void HighlightSpeaker(bool isPlayer)
    {
        // Kill tween cũ để tránh conflict nếu spam nút
        playerColorTween?.Kill();
        npcColorTween?.Kill();

        // Player: Nếu là Player nói -> Màu sáng, ngược lại -> Màu tối
        Color targetPlayerColor = isPlayer ? activeColor : inactiveColor;
        // NPC: Nếu KHÔNG phải Player nói -> Màu sáng, ngược lại -> Màu tối
        Color targetNpcColor = !isPlayer ? activeColor : inactiveColor;

        // Thực hiện Tween
        playerColorTween = playerPortrait.DOColor(targetPlayerColor, fadeDuration);
        npcColorTween = npcPortrait.DOColor(targetNpcColor, fadeDuration);

        // Optional: Scale nhẹ lên để tạo điểm nhấn
        playerPortrait.transform.DOScale(isPlayer ? 1.1f : 1.0f, fadeDuration);
        npcPortrait.transform.DOScale(!isPlayer ? 1.1f : 1.0f, fadeDuration);
    }

    public void ShowChoices(List<DialogueChoiceNode.ChoiceOption> options, Action<int> onSelected)
    {
        ClearChoices();
        for (int i = 0; i < options.Count; i++)
        {
            int index = i;
            var btn = Instantiate(choiceButtonPrefab, choiceContainer);
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = options[i].Text;
            btn.onClick.AddListener(() => onSelected?.Invoke(index));
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
        foreach (var btn in activeChoices) if (btn != null) Destroy(btn.gameObject);
        activeChoices.Clear();
    }

    private void OnDisable()
    {
        typeWriterTween?.Kill();
        playerColorTween?.Kill();
        npcColorTween?.Kill();
    }
}