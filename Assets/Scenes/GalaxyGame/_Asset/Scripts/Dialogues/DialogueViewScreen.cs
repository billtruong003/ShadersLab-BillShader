using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

public class DialogueViewScreen : DialogueViewBase
{
    [Title("Screen Specific")]
    [SerializeField] private GameObject mainContainer;
    [SerializeField] private Image playerPortrait;
    [SerializeField] private Image npcPortrait;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    public override void Initialize()
    {
        mainContainer.SetActive(false);
    }

    public override void SetActive(bool active)
    {
        mainContainer.SetActive(active);
        if (!active) ClearChoices();
    }

    protected override void OnLineStart(CharacterProfile speaker)
    {
        UpdatePortraits(speaker);
    }

    private void UpdatePortraits(CharacterProfile speaker)
    {
        bool isPlayer = speaker.Type == CharacterType.Player;

        if (isPlayer && playerPortrait != null) playerPortrait.sprite = speaker.Portrait;
        else if (!isPlayer && npcPortrait != null) npcPortrait.sprite = speaker.Portrait;

        if (playerPortrait != null)
        {
            playerPortrait.DOColor(isPlayer ? activeColor : inactiveColor, fadeDuration);
            playerPortrait.transform.DOScale(isPlayer ? 1.1f : 1f, fadeDuration);
        }

        if (npcPortrait != null)
        {
            npcPortrait.DOColor(!isPlayer ? activeColor : inactiveColor, fadeDuration);
            npcPortrait.transform.DOScale(!isPlayer ? 1.1f : 1f, fadeDuration);
        }
    }
}