using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlaceableObject : MonoBehaviour
{
    public PlaceableObjectDataSO Data { get; private set; }
    public Quaternion Rotation { get; private set; }
    public Vector2Int GridCoordinates { get; private set; } // Chỉ Procedural Grid dùng
    public IReadOnlyList<PlacementCell> OccupiedCells => occupiedCells.AsReadOnly(); // Chỉ Zoned Grid dùng

    private PlacementGridBase placedOnGrid;
    private List<PlacementCell> occupiedCells = new List<PlacementCell>();

    public void Initialize(PlaceableObjectDataSO data, Quaternion rotation, PlacementGridBase grid)
    {
        this.Data = data;
        this.Rotation = rotation;
        this.placedOnGrid = grid;
    }

    public void AssignOccupiedCells(List<PlacementCell> cells)
    {
        if (cells != null)
        {
            occupiedCells = cells;
        }
    }

    public void SetGridCoordinates(Vector2Int coords)
    {
        GridCoordinates = coords;
    }

    private void OnDestroy()
    {
        placedOnGrid?.FreeOccupiedCells(this);
    }
}