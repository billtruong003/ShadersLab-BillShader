using UnityEngine;

namespace ProceduralDungeon.Configuration
{
    [CreateAssetMenu(fileName = "RoomLayoutProfile", menuName = "Procedural Dungeon/Room Layout Profile")]
    public class RoomLayoutProfile : ScriptableObject
    {
        public Vector3Int dungeonBounds = new Vector3Int(30, 5, 30);

        [Header("Room Generation")]
        public int minRoomCount = 8;
        public int maxRoomCount = 12;
        public Vector3Int minRoomSize = new Vector3Int(3, 1, 3);
        public Vector3Int maxRoomSize = new Vector3Int(7, 3, 7);
        public int roomPlacementAttempts = 100;
    }
}