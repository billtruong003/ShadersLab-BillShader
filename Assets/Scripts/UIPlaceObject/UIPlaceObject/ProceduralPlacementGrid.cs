using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

public class ProceduralPlacementGrid : PlacementGridBase
{
    [Title("Procedural Settings")]
    [OnValueChanged("InitializeGrid")]
    [MinValue(1)]
    [SerializeField] private int width = 10;

    [OnValueChanged("InitializeGrid")]
    [MinValue(1)]
    [SerializeField] private int height = 10;

    [OnValueChanged("InitializeGrid")]
    [MinValue(0.1f)]
    [SerializeField] private float cellSize = 1f;

    [Tooltip("The gap between grid cells.")]
    [OnValueChanged("InitializeGrid")]
    [MinValue(0)]
    [SerializeField] private float spacing = 0f;

    private PlaceableObject[,] gridArray;
    private Vector3 gridBottomLeftLocal;
    private float TotalCellSize => cellSize + spacing;

    protected override void InitializeGridLogic()
    {
        gridArray = new PlaceableObject[width, height];
        float totalWidth = width * TotalCellSize - spacing;
        float totalHeight = height * TotalCellSize - spacing;
        gridBottomLeftLocal = new Vector3(-totalWidth, 0, -totalHeight) * 0.5f;
    }

    protected override void UpdateColliderBounds()
    {
        if (gridCollider == null) return;
        float totalWidth = width * cellSize + Mathf.Max(0, width - 1) * spacing;
        float totalHeight = height * cellSize + Mathf.Max(0, height - 1) * spacing;
        gridCollider.center = new Vector3(0, placementPlaneYOffset, 0);
        gridCollider.size = new Vector3(totalWidth, 0.1f, totalHeight);
    }

    // TRIỂN KHAI PHƯƠNG THỨC MỚI
    public override Vector3 GetCellCenterInWorld(Vector3 worldPosition)
    {
        Vector2Int gridCoords = WorldToGridCoordinates(worldPosition);
        return GridToWorldPositionCenter(gridCoords);
    }

    // ĐƠN GIẢN HÓA
    public override Vector3 GetSnappedWorldPosition(Vector3 worldPosition, PlaceableObjectDataSO objectData)
    {
        // worldPosition ở đây đã là trung tâm của ô, chỉ cần thêm offset
        Vector3 snappedWorldPos = worldPosition;
        snappedWorldPos.y = WorldPlacementPlaneY + objectData.PlacementYOffset;
        return snappedWorldPos;
    }

    public override bool AreCellsAvailable(Vector3 worldPosition, PlaceableObjectDataSO objectData, Quaternion rotation)
    {
        Vector2Int startCoords = WorldToGridCoordinates(worldPosition);
        Vector2Int rotatedSize = GetRotatedSize(objectData.Size, rotation);
        for (int x = 0; x < rotatedSize.x; x++)
        {
            for (int z = 0; z < rotatedSize.y; z++)
            {
                int checkX = startCoords.x + x;
                int checkZ = startCoords.y + z;
                if (!IsWithinBounds(checkX, checkZ) || gridArray[checkX, checkZ] != null)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public override List<PlacementCell> OccupyCells(PlaceableObject placedObject, Vector3 worldPosition, PlaceableObjectDataSO objectData, Quaternion rotation)
    {
        Vector2Int startCoords = WorldToGridCoordinates(worldPosition);
        Vector2Int rotatedSize = GetRotatedSize(objectData.Size, rotation);
        placedObject.SetGridCoordinates(startCoords);

        for (int x = 0; x < rotatedSize.x; x++)
        {
            for (int z = 0; z < rotatedSize.y; z++)
            {
                int gridX = startCoords.x + x;
                int gridZ = startCoords.y + z;
                if (IsWithinBounds(gridX, gridZ))
                {
                    gridArray[gridX, gridZ] = placedObject;
                }
            }
        }
        return null;
    }

    public override void FreeOccupiedCells(PlaceableObject placedObject)
    {
        Vector2Int startCoords = placedObject.GridCoordinates;
        Vector2Int rotatedSize = GetRotatedSize(placedObject.Data.Size, placedObject.Rotation);
        for (int x = 0; x < rotatedSize.x; x++)
        {
            for (int z = 0; z < rotatedSize.y; z++)
            {
                int gridX = startCoords.x + x;
                int gridZ = startCoords.y + z;
                if (IsWithinBounds(gridX, gridZ) && gridArray[gridX, gridZ] == placedObject)
                {
                    gridArray[gridX, gridZ] = null;
                }
            }
        }
    }

    private Vector2Int WorldToGridCoordinates(Vector3 worldPosition)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        Vector3 relativePos = localPos - gridBottomLeftLocal;

        int x = Mathf.Clamp(Mathf.FloorToInt(relativePos.x / TotalCellSize), 0, width - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(relativePos.z / TotalCellSize), 0, height - 1);
        return new Vector2Int(x, z);
    }

    private Vector3 GridToWorldPositionCenter(Vector2Int gridCoordinates)
    {
        Vector3 cellCenterLocal = new Vector3(gridCoordinates.x, 0, gridCoordinates.y) * TotalCellSize
                                + new Vector3(TotalCellSize * 0.5f, 0, TotalCellSize * 0.5f)
                                + gridBottomLeftLocal;
        return transform.TransformPoint(cellCenterLocal);
    }

    private bool IsWithinBounds(int x, int z) => x >= 0 && z >= 0 && x < width && z < height;

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        if (gridArray == null) InitializeGridLogic(); // Ensure gridArray is not null in Editor

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                bool isOccupied = gridArray != null && IsWithinBounds(x, z) && gridArray[x, z] != null;
                Gizmos.color = isOccupied ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0.5f, 0.3f);
                Vector3 localCenter = GridToWorldPositionCenter(new Vector2Int(x, z));
                Gizmos.DrawCube(transform.InverseTransformPoint(localCenter) + Vector3.up * placementPlaneYOffset, new Vector3(cellSize, 0.1f, cellSize));
            }
        }
    }
}