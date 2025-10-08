using UnityEngine;

namespace ProceduralDungeon
{
    public class Room
    {
        public int Id { get; }
        public BoundsInt Bounds { get; }
        public Vector3 Center => Bounds.center;

        private static int nextId = 0;

        public Room(BoundsInt bounds)
        {
            Id = nextId++;
            Bounds = bounds;
        }
    }
}