using UnityEngine;
using System.Collections.Generic;
using System;

// Định nghĩa các loại đối tượng có thể đặt trên lưới, bao gồm cả nền và lỗ hổng
public enum ObjectType
{
    Void,       // Lỗ hổng, không có gì cả
    Ground,     // Chỉ có mặt đất
    Collectible,
    Obstacle,
    DangerZone
}

[Serializable]
public class GridCell
{
    public Vector2Int gridPosition;
    public ObjectType objectType;

    public GridCell(int x, int y)
    {
        gridPosition = new Vector2Int(x, y);
        // Trạng thái mặc định khi tạo grid mới là mặt đất, không phải lỗ hổng
        objectType = ObjectType.Ground;
    }
}

[CreateAssetMenu(fileName = "GridLevelData_01", menuName = "ShadersLab/Grid Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Grid Dimensions")]
    public int gridWidth = 20;
    public int gridHeight = 20;
    public float cellSize = 1.0f;

    [Header("Grid Content")]
    public List<GridCell> cells = new List<GridCell>();

    public GridCell GetCell(int x, int y)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
        {
            return null;
        }
        return cells[y * gridWidth + x];
    }

    // Giữ lại dữ liệu cũ khi thay đổi kích thước grid
    public void InitializeOrResizeGrid()
    {
        var oldCells = new Dictionary<Vector2Int, ObjectType>();
        foreach (var cell in cells)
        {
            oldCells[cell.gridPosition] = cell.objectType;
        }

        cells = new List<GridCell>(gridWidth * gridHeight);
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                var newCell = new GridCell(x, y);
                if (oldCells.TryGetValue(new Vector2Int(x, y), out var oldType))
                {
                    newCell.objectType = oldType;
                }
                cells.Add(newCell);
            }
        }
    }

    private void OnEnable()
    {
        if (cells == null || cells.Count == 0 || cells.Count != gridWidth * gridHeight)
        {
            InitializeOrResizeGrid();
        }
    }
}