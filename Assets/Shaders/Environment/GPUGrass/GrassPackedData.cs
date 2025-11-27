using UnityEngine;
using Unity.Mathematics;

namespace OptimizeGrass
{
    // 32 Bytes per blade (Aligned to float4)
    public struct GrassInstance
    {
        public float3 position; // 12 bytes
        public float rotY;      // 4 bytes
        public float2 scale;    // 8 bytes (x: width, y: height)
        public uint colorSeed;  // 4 bytes
        public float padding;   // 4 bytes (align stride)
    }
}