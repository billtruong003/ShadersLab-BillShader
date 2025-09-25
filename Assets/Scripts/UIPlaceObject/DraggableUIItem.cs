using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DraggableUIItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private PlaceableObjectDataSO placeableObjectData;
    [SerializeField] private UIItemResetButton resetButton;

    private Image itemImage;
    private PlaceableObject placedInstance;
    private bool isPlaced => placedInstance != null;

    private void Awake()
    {
        itemImage = GetComponent<Image>();
        ResetToAvailable();
    }

    private void OnEnable()
    {
        PlacementEvents.OnPlacementSucceeded += HandlePlacementSuccess;
        PlacementEvents.OnPlacementFailed += HandlePlacementFailure;
        PlacementEvents.OnObjectRemoved += HandleObjectRemoval;
    }

    private void OnDisable()
    {
        PlacementEvents.OnPlacementSucceeded -= HandlePlacementSuccess;
        PlacementEvents.OnPlacementFailed -= HandlePlacementFailure;
        PlacementEvents.OnObjectRemoved -= HandleObjectRemoval;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced || placeableObjectData == null) return;

        PlacementEvents.OnRequestPlacement?.Invoke(placeableObjectData, this);
        SetRaycastTarget(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Intentionally left blank.
        // Logic is handled by PlacementSystem.
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        PlacementEvents.OnDragEnd?.Invoke();
        SetRaycastTarget(true);
    }

    private void HandlePlacementSuccess(DraggableUIItem sourceItem, PlaceableObject newPlacedObject)
    {
        if (sourceItem != this) return;

        placedInstance = newPlacedObject;
        SetVisualState(false);
        if (resetButton != null)
        {
            resetButton.gameObject.SetActive(true);
        }
    }

    private void HandlePlacementFailure(DraggableUIItem sourceItem)
    {
        if (sourceItem != this) return;

        ResetToAvailable();
    }

    private void HandleObjectRemoval(PlaceableObject removedObject)
    {
        if (placedInstance != null && placedInstance == removedObject)
        {
            ResetToAvailable();
        }
    }

    public void RequestPlacedObjectRemoval()
    {
        if (!isPlaced) return;

        PlacementEvents.OnRequestObjectRemoval?.Invoke(placedInstance);
    }

    public void ResetToAvailable()
    {
        placedInstance = null;
        SetVisualState(true);
        if (resetButton != null)
        {
            resetButton.gameObject.SetActive(false);
        }
    }

    private void SetRaycastTarget(bool isEnabled)
    {
        if (itemImage != null)
        {
            itemImage.raycastTarget = isEnabled;
        }
    }

    private void SetVisualState(bool isAvailable)
    {
        if (itemImage == null) return;

        var tempColor = itemImage.color;
        tempColor.a = isAvailable ? 1.0f : 0.4f;
        itemImage.color = tempColor;
        SetRaycastTarget(isAvailable);
    }
}