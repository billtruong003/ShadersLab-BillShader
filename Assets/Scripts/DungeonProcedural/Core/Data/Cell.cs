using UnityEngine;

namespace ProceduralDungeon
{
    public struct Cell
    {
        public Vector3Int Position { get; private set; }
        public CellType Type { get; set; }

        public Cell(Vector3Int position, CellType type)
        {
            Position = position;
            Type = type;
        }
    }
}