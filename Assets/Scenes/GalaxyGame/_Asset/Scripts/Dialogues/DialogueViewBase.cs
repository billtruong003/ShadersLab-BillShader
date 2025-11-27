using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;

public enum DialogueViewMode { ScreenSpace, WorldSpace }

public abstract class DialogueViewBase : MonoBehaviour
{
    [Title("Base Settings")]
    [SerializeField] protected float fadeDuration = 0.3f;
    [SerializeField] protected Transform choiceContainer;
    [SerializeField] protected Button choiceButtonPrefab;

    [Title("Text Components")]
    [SerializeField] protected TextMeshProUGUI nameText;
    [SerializeField] protected TextMeshProUGUI dialogueText;

    protected List<Button> activeChoices = new List<Button>();
    protected Tween typeWriterTween;

    public abstract void Initialize();
    public abstract void SetActive(bool active);
    protected abstract void OnLineStart(CharacterProfile speaker);

    public void ShowLine(CharacterProfile speaker, string text, float typeSpeed, Action onComplete)
    {
        ClearChoices();
        OnLineStart(speaker);

        if (nameText != null)
        {
            nameText.text = speaker.CharacterName;
            nameText.color = speaker.NameColor;
        }

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            typeWriterTween?.Kill();
            typeWriterTween = DOTween.To(() => string.Empty, x => dialogueText.text = x, text, text.Length * typeSpeed)
                .SetEase(Ease.Linear)
                .OnComplete(() => onComplete?.Invoke());
        }
    }

    public void ShowChoices(List<DialogueChoiceNode.ChoiceOption> options, Action<int> onSelected)
    {
        ClearChoices();
        if (choiceContainer != null) choiceContainer.gameObject.SetActive(true);

        for (int i = 0; i < options.Count; i++)
        {
            int index = i;
            var btn = Instantiate(choiceButtonPrefab, choiceContainer);
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = options[i].Text;

            btn.onClick.AddListener(() => onSelected?.Invoke(index));
            AnimateChoiceButton(btn, i);
            activeChoices.Add(btn);
        }
    }

    public void DisplayFullText(string text)
    {
        typeWriterTween?.Kill();
        if (dialogueText != null) dialogueText.text = text;
    }

    protected virtual void AnimateChoiceButton(Button btn, int index)
    {
        btn.transform.localScale = Vector3.one;
    }

    protected void ClearChoices()
    {
        foreach (var btn in activeChoices) if (btn != null) Destroy(btn.gameObject);
        activeChoices.Clear();
    }

    protected virtual void OnDisable()
    {
        typeWriterTween?.Kill();
    }
}