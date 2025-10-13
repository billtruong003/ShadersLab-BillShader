// Assets/Scripts/UI/FloatingText.cs
using UnityEngine;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;

namespace VoTanTuTien.UI
{
    [RequireComponent(typeof(TextMeshPro))]
    public class FloatingText : MonoBehaviour, IPoolableObject
    {
        [Required]
        [SerializeField] private TextMeshPro textMesh;

        [BoxGroup("Animation Settings")]
        [SerializeField] private float moveDistance = 2f;
        [BoxGroup("Animation Settings")]
        [SerializeField] private float moveDuration = 1.2f;
        [BoxGroup("Animation Settings")]
        [SerializeField] private float fadeOutDelay = 0.7f;

        private Transform cameraTransform;
        private Sequence activeSequence;

        private void Awake()
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            if (textMesh == null)
            {
                textMesh = GetComponent<TextMeshPro>();
            }
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;
            transform.LookAt(transform.position + cameraTransform.forward);
        }

        public void SetText(string text, Color color)
        {
            textMesh.text = text;
            textMesh.color = color;
        }

        public void OnObjectSpawn()
        {
            activeSequence?.Kill();
            textMesh.alpha = 1f;
            transform.localScale = Vector3.one; // Reset scale

            activeSequence = DOTween.Sequence();
            activeSequence.Append(transform.DOMoveY(transform.position.y + moveDistance, moveDuration).SetEase(Ease.OutCubic));
            activeSequence.Insert(fadeOutDelay, textMesh.DOFade(0, moveDuration - fadeOutDelay).SetEase(Ease.InQuad));
            activeSequence.OnComplete(() => ObjectPoolManager.Instance.ReturnToPool(gameObject));
            activeSequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void OnObjectReturn()
        {
            activeSequence?.Kill();
        }
    }
}