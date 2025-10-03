using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Thêm vào để sử dụng Button

public class InventoryUI : MonoBehaviour
{
    // --- THAY ĐỔI: XÓA BỎ [SerializeField] ---
    // Chúng ta sẽ lấy tham chiếu từ GameDataManager thay vì kéo thả
    private InventorySystem inventorySystem;

    [Header("UI References")]
    [SerializeField] private GameObject inventorySlotPrefab;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private Button sortButton; // Thêm tham chiếu cho nút Sort

    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

    private void Start()
    {
        // --- THAY ĐỔI LỚN: TRUY CẬP HỆ THỐNG QUA SINGLETON ---
        // Đây là cách tiếp cận đúng trong kiến trúc mới.
        inventorySystem = GameDataManager.Instance.InventorySystem;

        // Nếu inventorySystem vẫn null, có thể GameDataManager chưa được khởi tạo đúng cách.
        if (inventorySystem == null)
        {
            Debug.LogError("InventorySystem not found in GameDataManager. Ensure GameDataManager is in the scene and initialized first.");
            return; // Dừng thực thi để tránh lỗi
        }

        InitializeUI();
        inventorySystem.OnInventoryChanged += Redraw;

        // Gắn sự kiện cho nút Sort nếu có
        if (sortButton != null)
        {
            sortButton.onClick.AddListener(OnSortButtonClicked);
        }
    }

    private void OnDestroy()
    {
        // Luôn kiểm tra null trước khi hủy đăng ký sự kiện
        if (inventorySystem != null)
        {
            inventorySystem.OnInventoryChanged -= Redraw;
        }
        if (sortButton != null)
        {
            sortButton.onClick.RemoveListener(OnSortButtonClicked);
        }
    }

    private void OnSortButtonClicked()
    {
        inventorySystem.Sort();
    }

    private void InitializeUI()
    {
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIs.Clear();

        for (int i = 0; i < inventorySystem.InventorySlots.Count; i++)
        {
            GameObject slotInstance = Instantiate(inventorySlotPrefab, slotContainer);
            var slotUIComponent = slotInstance.GetComponent<InventorySlotUI>();
            slotUIComponent.Initialize(inventorySystem, i);
            slotUIs.Add(slotUIComponent);
        }
    }

    private void Redraw()
    {
        // Đảm bảo số lượng slot UI khớp với dữ liệu
        if (slotUIs.Count != inventorySystem.InventorySlots.Count)
        {
            InitializeUI(); // Nếu không khớp, khởi tạo lại toàn bộ UI
            return;
        }

        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].UpdateSlot(inventorySystem.GetSlotAt(i));
        }
    }
}