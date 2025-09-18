using UnityEngine;
using Sirenix.OdinInspector;

[ExecuteInEditMode]
[RequireComponent(typeof(Collider))]
public class PlacementGrid : MonoBehaviour
{
    [Title("Grid Configuration")]
    [OnValueChanged("InitializeGrid")]
    [SerializeField] private int width = 10;

    [OnValueChanged("InitializeGrid")]
    [SerializeField] private int height = 10;

    [OnValueChanged("InitializeGrid")]
    [SerializeField] private float cellSize = 1f;

    [Title("Debug")]
    [SerializeField] private bool showRotationAxis = true;

    private PlaceableObject[,] gridArray;
    private Vector3 gridBottomLeftLocal;

    private void OnEnable() => InitializeGrid();
    private void OnValidate() => InitializeGrid();

    [Button("Recalculate Grid Layout"), PropertyOrder(-1)]
    public void InitializeGrid()
    {
        gridArray = new PlaceableObject[width, height];
        gridBottomLeftLocal = new Vector3(-width * cellSize, 0, -height * cellSize) * 0.5f;
    }

    public Vector3 GetSnappedWorldPosition(Vector3 worldPosition)
    {
        Vector2Int gridCoords = WorldToGridCoordinates(worldPosition);
        return GridToWorldPositionCenter(gridCoords);
    }

    public Vector2Int WorldToGridCoordinates(Vector3 worldPosition)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        Vector3 relativePos = localPos - gridBottomLeftLocal;

        int x = Mathf.FloorToInt(relativePos.x / cellSize);
        int z = Mathf.FloorToInt(relativePos.z / cellSize);
        return new Vector2Int(x, z);
    }

    public Vector3 GridToWorldPositionCenter(Vector2Int gridCoordinates)
    {
        Vector3 cellCenterLocal = new Vector3(gridCoordinates.x, 0, gridCoordinates.y) * cellSize
                                + new Vector3(cellSize, 0, cellSize) * 0.5f
                                + gridBottomLeftLocal;

        return transform.TransformPoint(cellCenterLocal);
    }

    public bool AreCellsAvailable(Vector2Int startCoords, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                int checkX = startCoords.x + x;
                int checkZ = startCoords.y + z;
                if (!IsWithinBounds(checkX, checkZ) || gridArray[checkX, checkZ] != null) return false;
            }
        }
        return true;
    }

    public void OccupyCells(PlaceableObject placedObject, Vector2Int startCoords, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                int gridX = startCoords.x + x;
                int gridZ = startCoords.y + z;
                if (IsWithinBounds(gridX, gridZ)) gridArray[gridX, gridZ] = placedObject;
            }
        }
    }

    public void FreeCells(Vector2Int startCoords, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                int gridX = startCoords.x + x;
                int gridZ = startCoords.y + z;
                if (IsWithinBounds(gridX, gridZ)) gridArray[gridX, gridZ] = null;
            }
        }
    }

    public static Vector2Int GetRotatedSize(Vector2Int size, Quaternion rotation)
    {
        int yRotation = Mathf.RoundToInt(Mathf.Abs(rotation.eulerAngles.y));
        return (yRotation == 90 || yRotation == 270) ? new Vector2Int(size.y, size.x) : size;
    }

    private bool IsWithinBounds(int x, int z)
    {
        return x >= 0 && z >= 0 && x < width && z < height;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
        Vector3 localOrigin = gridBottomLeftLocal;
        for (int x = 0; x <= width; x++)
            Gizmos.DrawLine(localOrigin + new Vector3(x * cellSize, 0, 0), localOrigin + new Vector3(x * cellSize, 0, height * cellSize));
        for (int z = 0; z <= height; z++)
            Gizmos.DrawLine(localOrigin + new Vector3(0, 0, z * cellSize), localOrigin + new Vector3(width * cellSize, 0, z * cellSize));

        if (showRotationAxis)
        {
            Gizmos.color = Color.red;
            Vector3 gridCenter = Vector3.zero;
            Vector3 forward = Vector3.forward * (cellSize * 1.5f);
            Gizmos.DrawLine(gridCenter, gridCenter + forward);
            Gizmos.DrawSphere(gridCenter + forward, cellSize * 0.1f);
        }

        if (!Application.isPlaying || gridArray == null) return;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                PlaceableObject obj = gridArray[x, z];
                if (obj != null && obj.GridStartPosition == new Vector2Int(x, z))
                {
                    Vector3 cellCenterLocal = new Vector3(x, 0, z) * cellSize + new Vector3(cellSize, 0, cellSize) * 0.5f + gridBottomLeftLocal;
                    Gizmos.color = Color.blue;
                    Vector3 forward = obj.Rotation * Vector3.forward * (cellSize * 0.4f);
                    Gizmos.DrawLine(cellCenterLocal, cellCenterLocal + forward);
                    Gizmos.DrawSphere(cellCenterLocal + forward, cellSize * 0.05f);
                }
            }
        }
    }
}