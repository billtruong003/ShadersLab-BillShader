// File: GenericGrid.cs
using System.Collections.Generic;
using UnityEngine;

public class GenericGrid<T>
{
    public int Width { get; }
    public int Height { get; }
    public float CellSize { get; }
    public Vector3 Origin { get; }

    private readonly Dictionary<Vector2Int, T> gridObjects;

    public GenericGrid(int width, int height, float cellSize, Vector3 origin)
    {
        Width = width;
        Height = height;
        CellSize = cellSize;
        Origin = origin;
        gridObjects = new Dictionary<Vector2Int, T>();
    }

    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x, 0, gridPosition.y) * CellSize + Origin;
    }

    public Vector2Int GetGridCoordinates(Vector3 worldPosition)
    {
        Vector3 relativePos = worldPosition - Origin;
        int x = Mathf.FloorToInt(relativePos.x / CellSize);
        int y = Mathf.FloorToInt(relativePos.z / CellSize);
        return new Vector2Int(x, y);
    }

    public void SetValue(Vector2Int gridPosition, T value)
    {
        if (!IsWithinBounds(gridPosition)) return;
        gridObjects[gridPosition] = value;
    }

    public T GetValue(Vector2Int gridPosition)
    {
        gridObjects.TryGetValue(gridPosition, out T value);
        return value;
    }

    public bool IsWithinBounds(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < Width &&
               gridPosition.y >= 0 && gridPosition.y < Height;
    }
}