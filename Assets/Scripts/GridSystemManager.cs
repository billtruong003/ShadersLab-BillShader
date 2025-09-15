// File: GridSystemManager.cs
using UnityEngine;

public class GridSystemManager : MonoBehaviour
{
    [SerializeField] private int gridWidth = 50;
    [SerializeField] private int gridHeight = 50;
    [SerializeField] private float cellSize = 1f;

    private GenericGrid<PlaceableObject> grid;

    private void OnEnable()
    {
        SystemEvents.OnObjectPlaced += OnObjectPlaced;
    }
    
    private void OnDisable()
    {
        SystemEvents.OnObjectPlaced -= OnObjectPlaced;
    }

    public void Initialize()
    {
        grid = new GenericGrid<PlaceableObject>(gridWidth, gridHeight, cellSize, transform.position);
        FindObjectOfType<PlacementSystem>().Initialize(grid);
    }

    private void OnObjectPlaced(PlaceableObject placedObject, Vector2Int origin)
    {
        for (int x = 0; x < placedObject.Data.size.x; x++)
        {
            for (int y = 0; y < placedObject.Data.size.y; y++)
            {
                grid.SetValue(origin + new Vector2Int(x, y), placedObject);
            }
        }
    }
}



