using UnityEngine;

namespace ProceduralDungeon
{
    /// <summary>
    /// Provides useful extension methods for the BoundsInt struct.
    /// </summary>
    public static class BoundsIntExtensions
    {
        /// <summary>
        /// Checks if two BoundsInt objects are overlapping.
        /// This method is missing from the core BoundsInt API.
        /// </summary>
        /// <param name="a">The first bounds.</param>
        /// <param name="b">The second bounds.</param>
        /// <returns>True if they intersect, false otherwise.</returns>
        public static bool Intersects(this BoundsInt a, BoundsInt b)
        {
            // Two boxes intersect if they overlap on all three axes.
            // They do NOT intersect if there is any axis on which they are separate.

            bool overlapX = a.xMin < b.xMax && a.xMax > b.xMin;
            bool overlapY = a.yMin < b.yMax && a.yMax > b.yMin;
            bool overlapZ = a.zMin < b.zMax && a.zMax > b.zMin;

            return overlapX && overlapY && overlapZ;
        }
    }
}