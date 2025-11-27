using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

public class DialogueViewWorld : DialogueViewBase
{
    [Title("World Specific")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Vector3 offsetFromTarget = new Vector3(0, 0.5f, 0);
    [SerializeField] private float moveSmoothTime = 0.15f;

    private Transform currentTarget;
    private Vector3 currentVelocity;
    private Camera mainCam;

    public override void Initialize()
    {
        mainCam = Camera.main;
        if (worldCanvas != null) worldCanvas.worldCamera = mainCam;
        gameObject.SetActive(false);
    }

    public override void SetActive(bool active)
    {
        gameObject.SetActive(active);
        if (active)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeDuration);
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, fadeDuration).SetEase(Ease.OutBack);
        }
        else
        {
            ClearChoices();
        }
    }

    private void LateUpdate()
    {
        if (!gameObject.activeSelf) return;
        HandleBillboard();
        FollowTarget();
    }

    protected override void OnLineStart(CharacterProfile speaker)
    {
        UpdateTargetPosition(speaker);
        if (portraitImage != null)
        {
            portraitImage.sprite = speaker.Portrait;
            portraitImage.enabled = speaker.Portrait != null;
        }
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 1);
    }

    protected override void AnimateChoiceButton(Button btn, int index)
    {
        btn.transform.localScale = Vector3.zero;
        btn.transform.DOScale(Vector3.one, 0.2f).SetDelay(index * 0.1f).SetEase(Ease.OutBack);
    }

    private void UpdateTargetPosition(CharacterProfile speaker)
    {
        Transform speechPoint = DialogueActorRegistry.GetSpeechPoint(speaker);

        if (speechPoint == null)
        {
            currentTarget = null;
            transform.position = mainCam.transform.position + mainCam.transform.forward * 3f;
        }
        else if (currentTarget != speechPoint)
        {
            currentTarget = speechPoint;
            transform.DOMove(currentTarget.position + offsetFromTarget, 0.3f).SetEase(Ease.OutCubic);
        }
    }

    private void HandleBillboard()
    {
        transform.rotation = mainCam.transform.rotation;
    }

    private void FollowTarget()
    {
        if (currentTarget == null) return;
        Vector3 targetPos = currentTarget.position + offsetFromTarget;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, moveSmoothTime);
    }
}