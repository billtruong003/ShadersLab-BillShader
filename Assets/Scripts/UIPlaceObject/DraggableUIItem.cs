using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Image))]
public class DraggableUIItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Title("Dependencies")]
    [Required("A PlacementManager must be assigned.")]
    [SerializeField] private PlacementManager placementManager;

    [Title("Configuration")]
    [Required]
    [InlineEditor(InlineEditorModes.GUIAndHeader, Expanded = false)]
    [SerializeField] private PlaceableObjectDataSO placeableObjectData;

    [Required]
    [ChildGameObjectsOnly]
    [SerializeField] private UIItemResetButton resetButton;

    private Image itemImage;
    private bool isPlaced = false;

    private void Awake()
    {
        itemImage = GetComponent<Image>();
        ResetToAvailable();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced || placeableObjectData == null || placementManager == null) return;

        placementManager.OnBeginDragItem(placeableObjectData, this);
        itemImage.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced || placementManager == null) return;

        placementManager.OnEndDragItem();
        itemImage.raycastTarget = true;
    }

    public void NotifyPlacementSuccess()
    {
        isPlaced = true;
        SetVisualState(false);
        resetButton.gameObject.SetActive(true);
    }

    public void ResetToAvailable()
    {
        isPlaced = false;
        SetVisualState(true);
        resetButton.gameObject.SetActive(false);
    }

    private void SetVisualState(bool isAvailable)
    {
        var tempColor = itemImage.color;
        tempColor.a = isAvailable ? 1.0f : 0.4f;
        itemImage.color = tempColor;
        itemImage.raycastTarget = isAvailable;
    }
}