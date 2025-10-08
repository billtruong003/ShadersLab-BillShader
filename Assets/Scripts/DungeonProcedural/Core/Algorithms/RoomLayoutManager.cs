using System.Collections.Generic;
using UnityEngine;
using ProceduralDungeon.Configuration;

namespace ProceduralDungeon.Algorithms
{
    public class RoomLayoutManager
    {
        public List<Room> GenerateRooms(RoomLayoutProfile profile)
        {
            var rooms = new List<Room>();
            int roomCount = Random.Range(profile.minRoomCount, profile.maxRoomCount + 1);

            for (int i = 0; i < profile.roomPlacementAttempts && rooms.Count < roomCount; i++)
            {
                int roomWidth = Random.Range(profile.minRoomSize.x, profile.maxRoomSize.x + 1);
                int roomHeight = Random.Range(profile.minRoomSize.y, profile.maxRoomSize.y + 1);
                int roomDepth = Random.Range(profile.minRoomSize.z, profile.maxRoomSize.z + 1);

                int x = Random.Range(1, profile.dungeonBounds.x - roomWidth - 1);
                int y = Random.Range(1, profile.dungeonBounds.y - roomHeight - 1);
                int z = Random.Range(1, profile.dungeonBounds.z - roomDepth - 1);

                var newBounds = new BoundsInt(x, y, z, roomWidth, roomHeight, roomDepth);
                var newRoom = new Room(newBounds);

                if (!DoesOverlap(newRoom, rooms))
                {
                    rooms.Add(newRoom);
                }
            }
            return rooms;
        }

        private bool DoesOverlap(Room newRoom, List<Room> existingRooms)
        {
            var bufferedBounds = newRoom.Bounds;
            bufferedBounds.position -= Vector3Int.one;
            bufferedBounds.size += Vector3Int.one * 2;

            foreach (var existingRoom in existingRooms)
            {
                // This line now works correctly thanks to our new extension method.
                if (existingRoom.Bounds.Intersects(bufferedBounds))
                {
                    return true;
                }
            }
            return false;
        }
    }
}