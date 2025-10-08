using UnityEngine;
using System.Collections.Generic;
using ProceduralDungeon.Configuration;
using ProceduralDungeon.Algorithms;
using ProceduralDungeon.Visualization;

namespace ProceduralDungeon.Core
{
    [RequireComponent(typeof(DungeonVisualizer))]
    public class DungeonOrchestrator : MonoBehaviour
    {
        [SerializeField] private DungeonGenerationProfile generationProfile;

        private DungeonVisualizer _visualizer;

        private void Awake()
        {
            _visualizer = GetComponent<DungeonVisualizer>();
        }

        [ContextMenu("Generate Dungeon")]
        public void GenerateDungeon()
        {
            if (generationProfile == null || generationProfile.roomLayout == null || generationProfile.connectivity == null || generationProfile.theme == null)
            {
                Debug.LogError("Generation Profile or its sub-profiles are not assigned!");
                return;
            }

            var grid = new DungeonGrid(generationProfile.roomLayout.dungeonBounds);

            var roomLayoutManager = new RoomLayoutManager();
            List<Room> rooms = roomLayoutManager.GenerateRooms(generationProfile.roomLayout);

            CarveRooms(grid, rooms);

            var connectivityGenerator = new ConnectivityGraphGenerator();
            List<HallwayConnection> connections = connectivityGenerator.GenerateConnections(rooms, generationProfile.connectivity);

            var hallwayPathfinder = new HallwayPathfinder();
            hallwayPathfinder.CarveHallways(grid, connections);

            PlaceWalls(grid);

            _visualizer.Visualize(grid, generationProfile.theme);

            Debug.Log($"Dungeon generated with {rooms.Count} rooms.");
        }

        private void CarveRooms(DungeonGrid grid, List<Room> rooms)
        {
            foreach (var room in rooms)
            {
                for (int y = room.Bounds.yMin; y < room.Bounds.yMax; y++)
                {
                    for (int z = room.Bounds.zMin; z < room.Bounds.zMax; z++)
                    {
                        for (int x = room.Bounds.xMin; x < room.Bounds.xMax; x++)
                        {
                            // FIX: Get-Modify-Set pattern for struct
                            Cell cell = grid[x, y, z];
                            cell.Type = CellType.RoomFloor;
                            grid[x, y, z] = cell;
                        }
                    }
                }
            }
        }

        private void PlaceWalls(DungeonGrid grid)
        {
            for (int y = 0; y < grid.Size.y; y++)
            {
                for (int z = 0; z < grid.Size.z; z++)
                {
                    for (int x = 0; x < grid.Size.x; x++)
                    {
                        // No need for Get-Modify-Set if we read first
                        if (grid[x, y, z].Type == CellType.Empty)
                        {
                            if (HasAdjacentFloor(grid, new Vector3Int(x, y, z)))
                            {
                                // FIX: Get-Modify-Set pattern for struct
                                Cell cell = grid[x, y, z];
                                cell.Type = CellType.Wall;
                                grid[x, y, z] = cell;
                            }
                        }
                    }
                }
            }
        }

        private bool HasAdjacentFloor(DungeonGrid grid, Vector3Int position)
        {
            Vector3Int[] neighbors = {
                Vector3Int.right, Vector3Int.left,
                new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1), // Using new Vector3Int for clarity
                Vector3Int.up, Vector3Int.down
            };

            foreach (var dir in neighbors)
            {
                var checkPos = position + dir;
                if (checkPos.x < 0 || checkPos.x >= grid.Size.x ||
                    checkPos.y < 0 || checkPos.y >= grid.Size.y ||
                    checkPos.z < 0 || checkPos.z >= grid.Size.z) continue;

                var neighborType = grid[checkPos].Type;
                if (neighborType == CellType.RoomFloor || neighborType == CellType.Hallway)
                {
                    return true;
                }
            }
            return false;
        }
    }
}