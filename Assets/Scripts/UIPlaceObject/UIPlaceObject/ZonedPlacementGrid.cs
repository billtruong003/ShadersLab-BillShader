using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

public class ZonedPlacementGrid : PlacementGridBase
{
    [Title("Zoned Settings")]
    [InfoBox("This system uses child objects with the 'PlacementCell' component as valid placement locations.")]
    [SerializeField, ReadOnly]
    private List<PlacementCell> placementCells = new List<PlacementCell>();

    private Vector3 zonedCellDimensions = Vector3.one;

    protected override void InitializeGridLogic()
    {
        FindCellsInChildren();
        if (placementCells.Any(c => c != null))
        {
            var firstCellCollider = placementCells.FirstOrDefault(c => c != null)?.GetComponent<BoxCollider>();
            if (firstCellCollider != null)
            {
                zonedCellDimensions = firstCellCollider.size;
            }
        }
    }

    [Button("Find and Register Cells from Children")]
    private void FindCellsInChildren()
    {
        placementCells = GetComponentsInChildren<PlacementCell>().ToList();
        foreach (var cell in placementCells)
        {
            cell.RegisterToGrid(this);
        }
    }

    protected override void UpdateColliderBounds()
    {
        if (gridCollider == null || placementCells == null || !placementCells.Any(c => c != null)) return;

        var bounds = new Bounds(transform.InverseTransformPoint(placementCells.First(c => c != null).transform.position), Vector3.zero);
        foreach (var cell in placementCells.Where(c => c != null))
        {
            bounds.Encapsulate(transform.InverseTransformPoint(cell.transform.position));
        }
        gridCollider.center = bounds.center + Vector3.up * placementPlaneYOffset;
        gridCollider.size = bounds.size + new Vector3(zonedCellDimensions.x, 0.1f, zonedCellDimensions.z);
    }

    public override Vector3 GetCellCenterInWorld(Vector3 worldPosition)
    {
        // Đối với ZonedGrid, worldPosition đã là trung tâm của cell bị hit.
        // Không cần tính toán thêm.
        return worldPosition;
    }

    public override Vector3 GetSnappedWorldPosition(Vector3 anchorPosition, PlaceableObjectDataSO objectData)
    {
        return anchorPosition + new Vector3(0, objectData.PlacementYOffset, 0);
    }

    public override bool AreCellsAvailable(Vector3 worldPosition, PlaceableObjectDataSO objectData, Quaternion rotation)
    {
        var cellsInBounds = GetCellsInBounds(worldPosition, objectData, rotation);
        Vector2Int rotatedSize = GetRotatedSize(objectData.Size, rotation);
        int requiredCellCount = rotatedSize.x * rotatedSize.y;

        if (cellsInBounds.Count < requiredCellCount) return false;

        return cellsInBounds.All(cell => !cell.IsOccupied);
    }

    public override List<PlacementCell> OccupyCells(PlaceableObject placedObject, Vector3 worldPosition, PlaceableObjectDataSO objectData, Quaternion rotation)
    {
        var cellsToOccupy = GetCellsInBounds(worldPosition, objectData, rotation);
        foreach (var cell in cellsToOccupy)
        {
            cell.Occupy(placedObject);
        }
        placedObject.AssignOccupiedCells(cellsToOccupy);
        return cellsToOccupy;
    }

    public override void FreeOccupiedCells(PlaceableObject placedObject)
    {
        foreach (var cell in placedObject.OccupiedCells.Where(c => c != null && c.OccupyingObject == placedObject))
        {
            cell.Free();
        }
    }

    private List<PlacementCell> GetCellsInBounds(Vector3 worldPosition, PlaceableObjectDataSO objectData, Quaternion rotation)
    {
        Vector2Int rotatedSize = GetRotatedSize(objectData.Size, rotation);
        Vector3 boxHalfExtents = new Vector3(
            rotatedSize.x * zonedCellDimensions.x * 0.5f,
            zonedCellDimensions.y,
            rotatedSize.y * zonedCellDimensions.z * 0.5f);

        int cellLayer = 1 << LayerMask.NameToLayer("Default"); // THAY "Default" bằng layer của cell
        if (placementCells.Count > 0 && placementCells[0] != null)
        {
            cellLayer = 1 << placementCells[0].gameObject.layer;
        }

        Collider[] hitColliders = Physics.OverlapBox(worldPosition, boxHalfExtents, transform.rotation * rotation, cellLayer, QueryTriggerInteraction.Collide);

        return hitColliders
            .Select(col => col.GetComponent<PlacementCell>())
            .Where(cell => cell != null && placementCells.Contains(cell))
            .Distinct()
            .ToList();
    }
}