// File: PlacementSystem.cs
using UnityEngine;
using UnityEngine.EventSystems;
using System.Threading.Tasks;

public class PlacementSystem : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask placementLayerMask;
    [SerializeField] private float previewLerpSpeed = 10f;
    
    private IPlacementState currentState;
    private GenericGrid<PlaceableObject> grid;

    private void OnEnable()
    {
        SystemEvents.OnPlacementRequested += EnterPlacingState;
    }

    private void OnDisable()
    {
        SystemEvents.OnPlacementRequested -= EnterPlacingState;
    }
    
    public void Initialize(GenericGrid<PlaceableObject> mainGrid)
    {
        grid = mainGrid;
        TransitionTo(new InactiveState());
    }

    private void Update()
    {
        currentState?.Execute();
    }
    
    private void TransitionTo(IPlacementState nextState)
    {
        currentState?.Exit();
        currentState = nextState;
        currentState?.Enter();
    }
    
    private async void EnterPlacingState(PlaceableItemData data)
    {
        GameObject previewInstance = await AsyncObjectPooler.Instance.GetObject(data.prefabReference);
        TransitionTo(new PlacingState(this, grid, data, previewInstance));
    }

    // Lớp con: Trạng thái không hoạt động
    private class InactiveState : IPlacementState
    {
        public void Enter() { }
        public void Execute() { }
        public void Exit() { }
    }

    // Lớp con: Trạng thái đang đặt vật thể
    private class PlacingState : IPlacementState
    {
        private readonly PlacementSystem owner;
        private readonly GenericGrid<PlaceableObject> grid;
        private readonly PlaceableItemData itemData;
        private readonly GameObject previewInstance;
        private readonly Renderer previewRenderer;

        private Vector2Int lastGridPosition;
        private bool isPlacementValid;

        public PlacingState(PlacementSystem owner, GenericGrid<PlaceableObject> grid, PlaceableItemData itemData, GameObject previewInstance)
        {
            this.owner = owner;
            this.grid = grid;
            this.itemData = itemData;
            this.previewInstance = previewInstance;
            this.previewRenderer = previewInstance.GetComponentInChildren<Renderer>();
            lastGridPosition = new Vector2Int(int.MinValue, int.MinValue);
        }

        public void Enter()
        {
            if (previewRenderer != null && itemData.previewMaterial != null)
            {
                previewRenderer.material = itemData.previewMaterial;
            }
        }

        public void Execute()
        {
            HandlePreviewMovement();
            HandleConfirmation();
            HandleCancellation();
        }

        public void Exit()
        {
            AsyncObjectPooler.Instance.ReturnObject(itemData.prefabReference.AssetGUID, previewInstance);
        }

        private void HandlePreviewMovement()
        {
            Ray ray = owner.mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, owner.placementLayerMask))
            {
                Vector2Int gridPos = grid.GetGridCoordinates(hit.point);
                if (gridPos == lastGridPosition)
                {
                    SmoothMovePreview(grid.GetWorldPosition(gridPos));
                    return;
                }

                lastGridPosition = gridPos;
                isPlacementValid = CheckPlacementValidity(gridPos);
                UpdatePreviewVisuals();
            }
        }
        
        private void SmoothMovePreview(Vector3 targetPosition)
        {
             previewInstance.transform.position = Vector3.Lerp(
                previewInstance.transform.position, 
                targetPosition, 
                Time.deltaTime * owner.previewLerpSpeed);
        }

        private bool CheckPlacementValidity(Vector2Int origin)
        {
            for (int x = 0; x < itemData.size.x; x++)
            {
                for (int y = 0; y < itemData.size.y; y++)
                {
                    Vector2Int cell = origin + new Vector2Int(x, y);
                    if (!grid.IsWithinBounds(cell) || grid.GetValue(cell) != null)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void UpdatePreviewVisuals()
        {
            if (previewRenderer == null) return;
            Color color = isPlacementValid ? Color.green : Color.red;
            previewRenderer.material.color = new Color(color.r, color.g, color.b, 0.5f);
        }

        private void HandleConfirmation()
        {
            if (Input.GetMouseButtonDown(0) && isPlacementValid && !EventSystem.current.IsPointerOverGameObject())
            {
                PlaceObject();
                owner.TransitionTo(new InactiveState());
            }
        }

        private void HandleCancellation()
        {
            if (Input.GetMouseButtonDown(1))
            {
                SystemEvents.RaisePlacementCancelled();
                owner.TransitionTo(new InactiveState());
            }
        }

        private async void PlaceObject()
        {
            GameObject placedInstance = await AsyncObjectPooler.Instance.GetObject(itemData.prefabReference);
            placedInstance.transform.position = grid.GetWorldPosition(lastGridPosition);
            
            PlaceableObject placeable = placedInstance.GetComponent<PlaceableObject>() ?? placedInstance.AddComponent<PlaceableObject>();
            placeable.Initialize(itemData, lastGridPosition);
            
            SystemEvents.RaiseObjectPlaced(placeable, lastGridPosition);
        }
    }
}