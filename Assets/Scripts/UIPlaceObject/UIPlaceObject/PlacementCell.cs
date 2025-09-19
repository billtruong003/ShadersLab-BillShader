using UnityEngine;
using Sirenix.OdinInspector;

public class PlacementCell : MonoBehaviour
{
    [ShowInInspector, ReadOnly]
    public bool IsOccupied => OccupyingObject != null;

    [ShowInInspector, ReadOnly]
    public PlaceableObject OccupyingObject { get; private set; }

    [ShowInInspector, ReadOnly]
    private PlacementGridBase parentGrid;

    // THÊM MỚI: Cung cấp quyền truy cập vào grid cha
    public PlacementGridBase ParentGrid => parentGrid;

    public void RegisterToGrid(PlacementGridBase grid)
    {
        parentGrid = grid;
    }

    public void Occupy(PlaceableObject obj)
    {
        OccupyingObject = obj;
    }

    public void Free()
    {
        OccupyingObject = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? new Color(1, 0, 0, 0.4f) : new Color(0, 1, 0, 0.4f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}