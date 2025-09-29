// Path: Assets/Scripts/Managers/FloatingTextManager.cs
using UnityEngine;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    [SerializeField] private GameObject floatingTextPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Hàm Show giờ chỉ cần vị trí 3D
    public void Show(string text, Vector3 worldPosition)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogError("Floating Text Prefab is not assigned in the manager!");
            return;
        }

        GameObject textInstance = ObjectPoolManager.Instance.Spawn(floatingTextPrefab, worldPosition, Quaternion.identity);

        var floatingText = textInstance.GetComponent<FloatingText>();
        if (floatingText != null)
        {
            floatingText.SetText(text);
        }
        else
        {
            Debug.LogError("Floating Text Prefab is missing the FloatingText component!");
        }
    }
}