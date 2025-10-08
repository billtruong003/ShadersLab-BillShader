using UnityEngine;
using ProceduralDungeon.Configuration;

namespace ProceduralDungeon.Visualization
{
    public class DungeonVisualizer : MonoBehaviour
    {
        private Transform _dungeonRoot;
        private const string DungeonRootName = "[Dungeon]";

        public void Visualize(DungeonGrid grid, ThemingProfile theme)
        {
            ClearDungeon();
            _dungeonRoot = new GameObject(DungeonRootName).transform;

            for (int y = 0; y < grid.Size.y; y++)
            {
                for (int z = 0; z < grid.Size.z; z++)
                {
                    for (int x = 0; x < grid.Size.x; x++)
                    {
                        var cell = grid[x, y, z];
                        if (cell.Type == CellType.Empty) continue;

                        GameObject prefabToSpawn = GetPrefabForCellType(cell.Type, theme);
                        if (prefabToSpawn != null)
                        {
                            Instantiate(prefabToSpawn, new Vector3(x, y, z), Quaternion.identity, _dungeonRoot);
                        }
                    }
                }
            }
        }

        public void ClearDungeon()
        {
            var existingDungeon = GameObject.Find(DungeonRootName);
            if (existingDungeon != null)
            {
                DestroyImmediate(existingDungeon);
            }
        }

        private GameObject GetPrefabForCellType(CellType type, ThemingProfile theme)
        {
            switch (type)
            {
                case CellType.RoomFloor:
                case CellType.Hallway: // Using floor for hallways for now
                    return theme.floorPrefab;
                case CellType.Wall:
                    return theme.wallPrefab;
                case CellType.Door:
                    return theme.doorPrefab;
                case CellType.Staircase:
                    return theme.staircasePrefab;
                default:
                    return null;
            }
        }
    }
}