using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

public class PlacementManager : MonoBehaviour
{
    private enum SystemState { Idle, Placing, Removing }

    [Title("CONFIGURATION")]
    [Tooltip("This layer mask should exclusively contain the 'PlacementGridPlane' layer.")]
    [SerializeField] private LayerMask gridLayerMask;

    [Title("SYSTEM STATE")]
    [ShowInInspector, ReadOnly, EnumToggleButtons]
    private SystemState currentState = SystemState.Idle;

    [Title("RUNTIME REFERENCES")]
    [ShowInInspector, ReadOnly] private PlacementPreview activePreviewInstance;
    [ShowInInspector, ReadOnly] private DraggableUIItem sourceDragItem;
    [ShowInInspector, ReadOnly] private PlacementGrid targetGrid;

    private Camera mainCamera;
    private Quaternion currentRotation = Quaternion.identity;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1)) // Right-click to cancel
        {
            CancelCurrentOperation();
            return;
        }

        switch (currentState)
        {
            case SystemState.Placing:
                UpdatePlacementState();
                break;
            case SystemState.Removing:
                UpdateRemovalState();
                break;
        }
    }

    public void OnBeginDragItem(PlaceableObjectDataSO objectData, DraggableUIItem originatingUI)
    {
        if (currentState != SystemState.Idle || objectData?.PreviewPrefab == null) return;

        // SỬA LỖI: Chuyển trạng thái TRƯỚC khi khởi tạo đối tượng mới.
        // Điều này đảm bảo việc dọn dẹp trạng thái cũ không ảnh hưởng đến thiết lập của trạng thái mới.
        TransitionToState(SystemState.Placing);

        sourceDragItem = originatingUI;
        currentRotation = Quaternion.identity;

        GameObject previewObject = Instantiate(objectData.PreviewPrefab);
        activePreviewInstance = previewObject.GetComponent<PlacementPreview>();
        activePreviewInstance.Initialize(objectData);
    }

    public void OnEndDragItem()
    {
        if (currentState != SystemState.Placing || activePreviewInstance == null || targetGrid == null)
        {
            CancelCurrentOperation();
            return;
        }

        if (activePreviewInstance.IsPlacementValid && !IsPointerOverUI())
        {
            PlaceObjectOnGrid();
        }
        else
        {
            CancelCurrentOperation();
        }
    }

    public void EnterRemovalMode() => TransitionToState(SystemState.Removing);

    private void UpdatePlacementState()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotation *= Quaternion.Euler(0, 90, 0);
        }

        bool canFindPosition = TryCalculateGridPosition(out Vector3 finalPosition, out Vector2Int gridCoords, out bool isPlacementValid);

        UpdatePreview(canFindPosition, finalPosition, isPlacementValid);
    }

    private bool TryCalculateGridPosition(out Vector3 finalPosition, out Vector2Int gridCoords, out bool isPlacementValid)
    {
        finalPosition = Vector3.zero;
        gridCoords = Vector2Int.zero;
        isPlacementValid = false;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hit, 1000f, gridLayerMask) && hit.collider.TryGetComponent<PlacementGrid>(out targetGrid))
        {
            Plane gridPlane = new Plane(targetGrid.transform.up, targetGrid.transform.position);

            if (gridPlane.Raycast(ray, out float distance))
            {
                Vector3 preciseHitPoint = ray.GetPoint(distance);
                Vector3 snappedPosition = targetGrid.GetSnappedWorldPosition(preciseHitPoint);

                PlaceableObjectDataSO data = activePreviewInstance.GetObjectData();
                finalPosition = snappedPosition + (targetGrid.transform.up * data.PlacementYOffset);

                gridCoords = targetGrid.WorldToGridCoordinates(snappedPosition);
                Vector2Int rotatedSize = PlacementGrid.GetRotatedSize(data.Size, currentRotation);
                isPlacementValid = targetGrid.AreCellsAvailable(gridCoords, rotatedSize);

                return true;
            }
        }

        targetGrid = null;
        return false;
    }

    private void UpdatePreview(bool isVisible, Vector3 position, bool isValid)
    {
        if (activePreviewInstance == null) return;

        activePreviewInstance.SetVisibility(isVisible);
        if (isVisible)
        {
            activePreviewInstance.transform.position = position;
            activePreviewInstance.transform.rotation = targetGrid.transform.rotation * currentRotation;
            activePreviewInstance.UpdateVisuals(isValid);
        }
    }

    private void PlaceObjectOnGrid()
    {
        PlaceableObjectDataSO data = activePreviewInstance.GetObjectData();

        Vector3 onGridPosition = activePreviewInstance.transform.position - (targetGrid.transform.up * data.PlacementYOffset);
        Vector2Int gridCoords = targetGrid.WorldToGridCoordinates(onGridPosition);

        GameObject newObjectInstance = Instantiate(data.PrefabToPlace, activePreviewInstance.transform.position, activePreviewInstance.transform.rotation);
        PlaceableObject placeable = newObjectInstance.GetComponent<PlaceableObject>();

        placeable.Initialize(data, gridCoords, currentRotation, targetGrid);

        Vector2Int rotatedSize = PlacementGrid.GetRotatedSize(data.Size, currentRotation);
        targetGrid.OccupyCells(placeable, gridCoords, rotatedSize);

        sourceDragItem.NotifyPlacementSuccess();
        TransitionToState(SystemState.Idle);
    }

    private void UpdateRemovalState()
    {
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                if (hit.collider.TryGetComponent<PlaceableObject>(out var placeableObject))
                {
                    Destroy(hit.collider.gameObject);
                }
            }
        }
    }

    private void CancelCurrentOperation()
    {
        if (currentState == SystemState.Placing && sourceDragItem != null)
        {
            sourceDragItem.ResetToAvailable();
        }
        TransitionToState(SystemState.Idle);
    }

    private void TransitionToState(SystemState newState)
    {
        if (currentState == newState) return;

        CleanUpCurrentState();
        currentState = newState;
    }

    private void CleanUpCurrentState()
    {
        if (activePreviewInstance != null)
        {
            Destroy(activePreviewInstance.gameObject);
            activePreviewInstance = null;
        }
        sourceDragItem = null;
        targetGrid = null;
    }

    private bool IsPointerOverUI() => EventSystem.current.IsPointerOverGameObject();
}