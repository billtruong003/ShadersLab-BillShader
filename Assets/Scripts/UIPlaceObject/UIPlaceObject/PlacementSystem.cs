using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementSystem : MonoBehaviour
{
    private enum SystemState { Idle, Placing, Removing }

    [Header("Layer Masks")]
    [Tooltip("Layer mask for all placeable surfaces. Includes grid colliders and individual cell colliders.")]
    [SerializeField] private LayerMask _placementLayers;
    [SerializeField] private LayerMask _placeableObjectLayers;

    private SystemState _currentState = SystemState.Idle;
    private PlacementPreview _activePreview;
    private DraggableUIItem _sourceDragItem;
    private PlacementGridBase _targetGrid;
    private Camera _mainCamera;
    private Quaternion _currentRotation = Quaternion.identity;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        PlacementEvents.OnRequestPlacement += HandlePlacementRequest;
        PlacementEvents.OnDragEnd += HandleDragEnd;
        PlacementEvents.OnEnterRemovalMode += EnterRemovalMode;
        PlacementEvents.OnModeExited += CancelCurrentOperation;
    }

    private void OnDisable()
    {
        PlacementEvents.OnRequestPlacement -= HandlePlacementRequest;
        PlacementEvents.OnDragEnd -= HandleDragEnd;
        PlacementEvents.OnEnterRemovalMode -= EnterRemovalMode;
        PlacementEvents.OnModeExited -= CancelCurrentOperation;
    }

    private void Update()
    {
        switch (_currentState)
        {
            case SystemState.Placing:
                ProcessPlacementState();
                break;
            case SystemState.Removing:
                ProcessRemovalState();
                break;
        }
    }

    private void HandlePlacementRequest(PlaceableObjectDataSO objectData, DraggableUIItem originatingUI)
    {
        if (_currentState != SystemState.Idle || objectData?.PreviewPrefab == null) return;

        TransitionToState(SystemState.Placing);
        _sourceDragItem = originatingUI;
        _currentRotation = Quaternion.identity;

        GameObject previewObject = Instantiate(objectData.PreviewPrefab);
        _activePreview = previewObject.GetComponent<PlacementPreview>();
        _activePreview.Initialize(objectData);
    }

    private void HandleDragEnd()
    {
        if (_currentState != SystemState.Placing) return;

        bool isPointerOverUI = EventSystem.current.IsPointerOverGameObject();
        bool canPlace = _activePreview != null && _targetGrid != null && _activePreview.IsPlacementValid && !isPointerOverUI;

        if (canPlace)
        {
            PlaceObjectOnGrid();
        }
        else
        {
            CancelCurrentOperation();
        }
    }

    private void EnterRemovalMode() => TransitionToState(SystemState.Removing);

    private void ProcessPlacementState()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            _currentRotation *= Quaternion.Euler(0, 90, 0);
        }

        bool foundValidPlacementTarget = TryFindGridDataFromRay(out Vector3 anchorPosition);

        if (foundValidPlacementTarget)
        {
            PlaceableObjectDataSO data = _activePreview.GetObjectData();
            Vector3 finalPosition = _targetGrid.GetSnappedWorldPosition(anchorPosition, data);
            bool isPlacementValid = _targetGrid.AreCellsAvailable(finalPosition, data, _currentRotation);
            UpdatePreviewTransformAndState(true, finalPosition, isPlacementValid);
        }
        else
        {
            UpdatePreviewTransformAndState(false, Vector3.zero, false);
        }
    }

    private bool TryFindGridDataFromRay(out Vector3 anchorPosition)
    {
        anchorPosition = Vector3.zero;
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, 1000f, _placementLayers))
        {
            if (hitInfo.collider.TryGetComponent<PlacementCell>(out var cell))
            {
                // Zoned Grid: Điểm neo là trung tâm của cell, cực kỳ ổn định.
                _targetGrid = cell.ParentGrid;
                anchorPosition = cell.transform.position;
                return _targetGrid != null;
            }

            if (hitInfo.collider.TryGetComponent<PlacementGridBase>(out var grid))
            {
                // Procedural Grid: Yêu cầu grid tự tính toán điểm neo ổn định.
                _targetGrid = grid;
                anchorPosition = _targetGrid.GetCellCenterInWorld(hitInfo.point);
                return true;
            }
        }

        _targetGrid = null;
        return false;
    }

    private void UpdatePreviewTransformAndState(bool isVisible, Vector3 position, bool isValid)
    {
        if (_activePreview == null) return;

        _activePreview.SetVisibility(isVisible);
        if (isVisible && _targetGrid != null)
        {
            _activePreview.transform.position = position;
            _activePreview.transform.rotation = _targetGrid.transform.rotation * _currentRotation;
            _activePreview.UpdateVisuals(isValid);
        }
    }

    private void PlaceObjectOnGrid()
    {
        PlaceableObjectDataSO data = _activePreview.GetObjectData();
        Vector3 finalPosition = _activePreview.transform.position;
        Quaternion finalRotation = _activePreview.transform.rotation;

        GameObject newObjectInstance = Instantiate(data.PrefabToPlace, finalPosition, finalRotation);
        PlaceableObject placeable = newObjectInstance.GetComponent<PlaceableObject>();

        placeable.Initialize(data, _currentRotation, _targetGrid);
        _targetGrid.OccupyCells(placeable, finalPosition, data, _currentRotation);

        PlacementEvents.OnPlacementSucceeded?.Invoke(data, placeable);
        TransitionToState(SystemState.Idle);
    }

    private void ProcessRemovalState()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _placeableObjectLayers))
            {
                if (hit.collider.TryGetComponent<PlaceableObject>(out var placeableObject))
                {
                    PlacementEvents.OnObjectRemoved?.Invoke(placeableObject.Data);
                    Destroy(placeableObject.gameObject);
                }
            }
        }
    }

    private void CancelCurrentOperation()
    {
        if (_currentState == SystemState.Placing && _sourceDragItem != null)
        {
            PlacementEvents.OnPlacementFailed?.Invoke(_activePreview.GetObjectData());
        }
        TransitionToState(SystemState.Idle);
    }

    private void TransitionToState(SystemState newState)
    {
        if (_currentState == newState) return;

        CleanUpCurrentState();
        _currentState = newState;
    }

    private void CleanUpCurrentState()
    {
        if (_activePreview != null)
        {
            Destroy(_activePreview.gameObject);
            _activePreview = null;
        }
        _sourceDragItem = null;
        _targetGrid = null;
    }
}