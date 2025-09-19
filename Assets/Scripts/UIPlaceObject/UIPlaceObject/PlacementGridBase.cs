using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[RequireComponent(typeof(BoxCollider))]
public abstract class PlacementGridBase : MonoBehaviour
{
    [Title("Base Settings")]
    [Tooltip("The local Y-offset from the grid's origin for the placement plane.")]
    [SerializeField] protected float placementPlaneYOffset = 0.0f;

    [Title("Collider Settings")]
    [InfoBox("The BoxCollider will be automatically resized to fit the grid area.")]
    [SerializeField, Required, HideLabel, ReadOnly]
    protected BoxCollider gridCollider;

    [Title("Debug")]
    [SerializeField] protected bool showGizmos = true;

    public float WorldPlacementPlaneY => transform.position.y + placementPlaneYOffset;

    protected virtual void OnEnable() => InitializeGrid();

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (gridCollider == null) gridCollider = GetComponent<BoxCollider>();
        InitializeGrid();
    }
#endif

    [Button("Force Recalculate Grid"), PropertyOrder(-1)]
    public void InitializeGrid()
    {
        if (gridCollider == null) gridCollider = GetComponent<BoxCollider>();
        InitializeGridLogic();
        UpdateColliderBounds();
        gridCollider.isTrigger = true;
    }

    protected abstract void InitializeGridLogic();
    protected abstract void UpdateColliderBounds();

    // MỚI: Phương thức trừu tượng để lấy điểm neo ổn định
    public abstract Vector3 GetCellCenterInWorld(Vector3 worldPosition);
    public abstract Vector3 GetSnappedWorldPosition(Vector3 worldPosition, PlaceableObjectDataSO objectData);
    public abstract bool AreCellsAvailable(Vector3 worldPosition, PlaceableObjectDataSO objectData, Quaternion rotation);
    public abstract List<PlacementCell> OccupyCells(PlaceableObject placedObject, Vector3 worldPosition, PlaceableObjectDataSO objectData, Quaternion rotation);
    public abstract void FreeOccupiedCells(PlaceableObject placedObject);

    public static Vector2Int GetRotatedSize(Vector2Int size, Quaternion rotation)
    {
        int yRotation = Mathf.RoundToInt(Mathf.Abs(rotation.eulerAngles.y));
        bool isSwapped = (yRotation == 90 || yRotation == 270);
        return isSwapped ? new Vector2Int(size.y, size.x) : size;
    }
}