using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DraggableUIItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private PlaceableObjectDataSO placeableObjectData;
    [SerializeField] private UIItemResetButton resetButton;

    private Image itemImage;
    private bool isPlaced = false;

    private void Awake()
    {
        itemImage = GetComponent<Image>();
        if (resetButton != null)
        {
            resetButton.gameObject.SetActive(false);
        }
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

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced) return;
        PlacementEvents.OnDragEnd?.Invoke();
        SetRaycastTarget(true);
    }

    private void HandlePlacementSuccess(PlaceableObjectDataSO data, PlaceableObject placedObject)
    {
        if (data == placeableObjectData)
        {
            isPlaced = true;
            SetVisualState(false);
            if (resetButton != null)
            {
                resetButton.gameObject.SetActive(true);
            }
        }
    }

    private void HandlePlacementFailure(PlaceableObjectDataSO data)
    {
        if (data == placeableObjectData)
        {
            ResetToAvailable();
        }
    }

    private void HandleObjectRemoval(PlaceableObjectDataSO data)
    {
        if (data == placeableObjectData && isPlaced)
        {
            ResetToAvailable();
        }
    }

    public void ResetToAvailable()
    {
        isPlaced = false;
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