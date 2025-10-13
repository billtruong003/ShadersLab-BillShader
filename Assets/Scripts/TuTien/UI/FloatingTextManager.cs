// Assets/Scripts/UI/FloatingTextManager.cs
using UnityEngine;
using Sirenix.OdinInspector;

namespace VoTanTuTien.UI
{
    public class FloatingTextManager : MonoBehaviour
    {
        // Thay đổi: Biến Instance giờ có private set để chỉ có thể được gán từ bên trong class này
        public static FloatingTextManager Instance { get; private set; }

        [Required]
        [SerializeField] private GameObject floatingTextPrefab;

        [Title("Màu Sắc Mặc Định")]
        [SerializeField] private Color damageColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color linhLucColor = new Color(0.4f, 0.9f, 1f);
        [SerializeField] private Color linhNangColor = new Color(0.8f, 0.6f, 1f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Một FloatingTextManager khác đã tồn tại. Hủy bỏ instance này.");
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        // Thay đổi: Thêm một hàm helper để kiểm tra sự tồn tại của Instance
        private static bool IsInstanceAvailable(string callingMethod)
        {
            if (Instance == null)
            {
                Debug.LogError($"FloatingTextManager.Instance không tồn tại trong scene. Lỗi gọi từ: {callingMethod}. Vui lòng thêm FloatingTextManager vào một GameObject.");
                return false;
            }
            return true;
        }

        public void ShowText(string text, Vector3 position, Color color)
        {
            if (!IsInstanceAvailable(nameof(ShowText)) || floatingTextPrefab == null) return;

            GameObject textObject = ObjectPoolManager.Instance.Spawn(floatingTextPrefab, position, Quaternion.identity);
            var floatingText = textObject.GetComponent<FloatingText>();
            if (floatingText != null)
            {
                floatingText.SetText(text, color);
            }
        }

        public void ShowDamage(float amount, Vector3 position)
        {
            if (IsInstanceAvailable(nameof(ShowDamage)))
                ShowText(Mathf.FloorToInt(amount).ToString(), position, damageColor);
        }

        public void ShowLinhLucGain(long amount, Vector3 position)
        {
            if (IsInstanceAvailable(nameof(ShowLinhLucGain)))
                ShowText($"+{amount} Linh Lực", position, linhLucColor);
        }

        public void ShowLinhNangGain(long amount, Vector3 position)
        {
            if (IsInstanceAvailable(nameof(ShowLinhNangGain)))
                ShowText($"+{amount} Linh Năng", position, linhNangColor);
        }
    }
}