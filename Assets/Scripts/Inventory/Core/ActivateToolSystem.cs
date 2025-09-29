using UnityEngine;
using System;

public class ActiveToolSystem : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private int hotbarSize = 9;

    public int ActiveSlotIndex { get; private set; } = 0;
    public ItemData CurrentActiveItem => playerInventory.GetItemAt(ActiveSlotIndex);

    public event Action OnActiveSlotChanged;

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        for (int i = 0; i < hotbarSize; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SetActiveSlot(i);
                break;
            }
        }
    }

    public void SetActiveSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= hotbarSize) return;
        if (ActiveSlotIndex == slotIndex) return;

        ActiveSlotIndex = slotIndex;
        Debug.Log($"Active slot changed to: {ActiveSlotIndex + 1}");
        OnActiveSlotChanged?.Invoke();
    }
}