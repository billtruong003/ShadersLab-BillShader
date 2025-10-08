using UnityEngine;

namespace ProceduralDungeon
{
    public class DungeonGrid
    {
        private readonly Cell[,,] cells;
        public Vector3Int Size { get; }

        public DungeonGrid(Vector3Int size)
        {
            Size = size;
            cells = new Cell[size.x, size.y, size.z];

            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    for (int x = 0; x < size.x; x++)
                    {
                        cells[x, y, z] = new Cell(new Vector3Int(x, y, z), CellType.Empty);
                    }
                }
            }
        }

        public Cell this[int x, int y, int z]
        {
            get => cells[x, y, z];
            set => cells[x, y, z] = value;
        }

        public Cell this[Vector3Int position]
        {
            get => cells[position.x, position.y, position.z];
            set => cells[position.x, position.y, position.z] = value;
        }
    }
}