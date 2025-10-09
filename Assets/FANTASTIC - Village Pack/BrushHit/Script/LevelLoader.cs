using Sirenix.OdinInspector;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class ObjectPrefabMapping
{
    public ObjectType type;
    public GameObject prefab;
}

[HideMonoScript]
public class LevelLoader : SerializedMonoBehaviour
{
    [Title("Core Prefabs")]
    [Required("Cần prefab cho mặt đất.")]
    [SerializeField] private GameObject groundPrefab;

    [Title("Object Prefab Mapping")]
    [InfoBox("Gán prefab cho các đối tượng sẽ được đặt TRÊN mặt đất.")]
    [SerializeField] private List<ObjectPrefabMapping> objectMappings;

    [Title("Parent Transforms")]
    [SerializeField] private Transform levelRoot;

    [Title("Fallback Level Data")]
    [InfoBox("Level này sẽ được tải nếu không có level nào được truyền từ Main Menu. Dùng để test trực tiếp trong Scene.")]
    [SerializeField] private LevelData fallbackLevelData;

    private LevelData levelToLoad;
    private Dictionary<ObjectType, GameObject> objectPrefabDictionary;

    private void Awake()
    {
        if (!TryToAcquireLevelData()) return;

        InitializePrefabDictionary();
        GenerateLevelFromGridData();
    }

    private bool TryToAcquireLevelData()
    {
        // Ưu tiên level được truyền từ Main Menu qua GameDataPersistence
        if (GameDataPersistence.Instance != null && GameDataPersistence.Instance.LevelToLoad != null)
        {
            levelToLoad = GameDataPersistence.Instance.LevelToLoad;
        }
        else
        {
            // Nếu không, sử dụng level gán sẵn trong Inspector để test
            levelToLoad = fallbackLevelData;
        }

        if (levelToLoad != null) return true;

        Debug.LogError("Không có LevelData để tải. Vui lòng chọn màn chơi từ Menu chính hoặc gán một 'Fallback Level Data' trong Inspector.", this);
        enabled = false;
        return false;
    }

    private void InitializePrefabDictionary()
    {
        objectPrefabDictionary = objectMappings.ToDictionary(mapping => mapping.type, mapping => mapping.prefab);
    }

    private void GenerateLevelFromGridData()
    {
        ClearExistingLevel();

        foreach (GridCell cell in levelToLoad.cells)
        {
            if (cell.objectType == ObjectType.Void) continue;

            Vector3 spawnPosition = CalculateCenteredWorldPosition(cell.gridPosition);

            if (groundPrefab != null)
            {
                Instantiate(groundPrefab, spawnPosition, Quaternion.identity, levelRoot);
            }

            if (objectPrefabDictionary.TryGetValue(cell.objectType, out GameObject prefabToSpawn) && prefabToSpawn != null)
            {
                Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, levelRoot);
            }
        }
    }

    private Vector3 CalculateCenteredWorldPosition(Vector2Int gridPos)
    {
        float totalWidth = levelToLoad.gridWidth * levelToLoad.cellSize;
        float totalHeight = levelToLoad.gridHeight * levelToLoad.cellSize;
        Vector3 gridOrigin = new Vector3(-totalWidth / 2f, 0, -totalHeight / 2f);
        float cellCenterX = (gridPos.x * levelToLoad.cellSize) + (levelToLoad.cellSize / 2f);
        float cellCenterZ = (gridPos.y * levelToLoad.cellSize) + (levelToLoad.cellSize / 2f);
        return gridOrigin + new Vector3(cellCenterX, 0, cellCenterZ);
    }

    private void ClearExistingLevel()
    {
        if (levelRoot == null) levelRoot = transform;
        foreach (Transform child in levelRoot) Destroy(child.gameObject);
    }
}