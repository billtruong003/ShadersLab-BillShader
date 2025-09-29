// Path: Assets/Scripts/UI/FloatingText.cs
using UnityEngine;
using TMPro;
using DG.Tweening;

public class FloatingText : MonoBehaviour, IPoolableObject
{
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private float moveDistance = 1.5f;
    [SerializeField] private float moveDuration = 0.8f;
    [SerializeField] private float fadeOutDelay = 0.5f;

    private Transform cameraTransform;
    private Sequence activeSequence;

    private void Awake()
    {
        // Tối ưu: Lấy tham chiếu camera một lần
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // --- ĐÂY LÀ PHẦN SỬA LỖI QUAN TRỌNG ---
        // Tự động lấy TextMeshPro component nếu nó chưa được gán trong Inspector.
        // Điều này làm cho script trở nên mạnh mẽ và ít bị lỗi do cấu hình sai.
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshPro>();
            if (textMesh == null)
            {
                Debug.LogError("FloatingText prefab is missing a TextMeshPro component!", this);
                // Vô hiệu hóa để tránh lỗi lặp đi lặp lại
                this.enabled = false;
            }
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null || !this.enabled) return;

        // Kỹ thuật Billboarding: Luôn xoay object hướng về phía camera
        transform.LookAt(transform.position + cameraTransform.forward);
    }

    public void OnObjectSpawn()
    {
        if (!this.enabled) return;

        // Giết sequence cũ nếu nó còn tồn tại để tránh lỗi
        activeSequence?.Kill();

        // Reset lại trạng thái
        textMesh.alpha = 1f;

        // Tạo sequence animation 3D mới
        activeSequence = DOTween.Sequence();
        activeSequence.Append(transform.DOMoveY(transform.position.y + moveDistance, moveDuration).SetEase(Ease.OutQuad));
        activeSequence.Insert(fadeOutDelay, textMesh.DOFade(0, moveDuration - fadeOutDelay));
        activeSequence.OnComplete(() => ObjectPoolManager.Instance.ReturnToPool(gameObject));
        activeSequence.SetLink(gameObject); // Đảm bảo tween bị hủy nếu object bị hủy bất ngờ
    }

    public void SetText(string text)
    {
        if (textMesh != null)
        {
            textMesh.text = text;
        }
    }

    public void OnObjectReturn()
    {
        // Khi trả về pool, hủy tween để đảm bảo không có lỗi
        activeSequence?.Kill();
    }
}