using System.Collections.Generic;
using UnityEngine;

namespace ProceduralDungeon.Algorithms
{
    public class HallwayPathfinder
    {
        public void CarveHallways(DungeonGrid grid, List<HallwayConnection> connections)
        {
            foreach (var connection in connections)
            {
                CarveSimplePath(grid, Vector3Int.RoundToInt(connection.roomA.Center), Vector3Int.RoundToInt(connection.roomB.Center));
            }
        }

        private void CarveSimplePath(DungeonGrid grid, Vector3Int start, Vector3Int end)
        {
            Vector3Int current = start;
            while (current != end)
            {
                // FIX: Get-Modify-Set pattern for struct
                Cell cell = grid[current];
                if (cell.Type == CellType.Empty)
                {
                    cell.Type = CellType.Hallway;
                    grid[current] = cell;
                }

                Vector3Int direction = end - current;

                // Prioritize horizontal movement
                if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.z) && direction.x != 0)
                {
                    current.x += (int)Mathf.Sign(direction.x);
                }
                else if (direction.z != 0)
                {
                    current.z += (int)Mathf.Sign(direction.z);
                }
                // Then vertical movement
                else if (direction.y != 0)
                {
                    current.y += (int)Mathf.Sign(direction.y);
                }
            }
        }
    }
}