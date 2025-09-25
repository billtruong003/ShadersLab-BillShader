using UnityEngine;
using System.Collections.Generic;

public class PlaceableObject : MonoBehaviour
{
    public PlaceableObjectDataSO Data { get; private set; }
    public Quaternion Rotation { get; private set; }
    public DraggableUIItem SourceUIItem { get; private set; }
    public PlacementGridBase placedOnGrid { get; private set; }

    // Procedural Grid Data
    public Vector2Int GridCoordinates { get; private set; }

    // Zoned Grid Data
    private readonly List<PlacementCell> occupiedCells = new List<PlacementCell>();
    public IReadOnlyList<PlacementCell> OccupiedCells => occupiedCells.AsReadOnly();

    public void Initialize(PlaceableObjectDataSO data, Quaternion rotation, PlacementGridBase grid, DraggableUIItem sourceItem)
    {
        this.Data = data;
        this.Rotation = rotation;
        this.placedOnGrid = grid;
        this.SourceUIItem = sourceItem;
    }

    public void AssignOccupiedCells(List<PlacementCell> cells)
    {
        if (cells != null)
        {
            occupiedCells.Clear();
            occupiedCells.AddRange(cells);
        }
    }

    public void SetGridCoordinates(Vector2Int coords)
    {
        GridCoordinates = coords;
    }
}