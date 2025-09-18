using UnityEngine;

public class PlaceableObject : MonoBehaviour
{
    public Vector2Int GridStartPosition { get; private set; }
    public Vector2Int Size { get; private set; }
    public Quaternion Rotation { get; private set; }

    private PlacementGrid placedOnGrid;

    public void Initialize(PlaceableObjectDataSO data, Vector2Int gridPosition, Quaternion rotation, PlacementGrid grid)
    {
        GridStartPosition = gridPosition;
        Size = data.Size;
        Rotation = rotation;
        placedOnGrid = grid;
    }

    private void OnDestroy()
    {
        if (placedOnGrid != null)
        {
            Vector2Int rotatedSize = PlacementGrid.GetRotatedSize(Size, Rotation);
            placedOnGrid.FreeCells(GridStartPosition, rotatedSize);
        }
    }
}