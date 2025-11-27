#ifndef BAKED_GRASS_SHARED
#define BAKED_GRASS_SHARED

struct GrassInstance {
    float3 position;
    float rotY;
    float2 scale;
    uint colorSeed;
    float padding;
};

#endif