using System;
using UnityEngine;

namespace MonsterLegion.Data
{
    [Serializable]
    public class StructureInstance
    {
        [Tooltip("ID của loại công trình, liên kết đến StructureSO.")]
        public string StructureID;

        [Tooltip("ID duy nhất của công trình này.")]
        public long InstanceID;

        [Tooltip("Cấp độ hiện tại của công trình.")]
        public int Level;

        [Tooltip("Vị trí của công trình trên lưới (grid).")]
        public Vector2Int GridPosition;

        public StructureInstance(string structureID, Vector2Int gridPosition)
        {
            StructureID = structureID;
            GridPosition = gridPosition;
            InstanceID = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Level = 1;
        }
    }
}