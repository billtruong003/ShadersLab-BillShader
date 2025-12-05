#ifndef INSTANCING_HELPERS_INCLUDED
#define INSTANCING_HELPERS_INCLUDED

struct CompressedInstanceData
{
    float3 position;
    float3 scale;
    float4 rotation;
    float2 lodRange; // x: minDistanceSq, y: maxDistanceSq
};

float4x4 TRS_To_Matrix(float3 position, float4 rotation, float3 scale)
{
    float x2 = rotation.x + rotation.x;
    float y2 = rotation.y + rotation.y;
    float z2 = rotation.z + rotation.z;
    float xx = rotation.x * x2;
    float xy = rotation.x * y2;
    float xz = rotation.x * z2;
    float yy = rotation.y * y2;
    float yz = rotation.y * z2;
    float zz = rotation.z * z2;
    float wx = rotation.w * x2;
    float wy = rotation.w * y2;
    float wz = rotation.w * z2;

    float4x4 m;
    m[0][0] = (1.0 - (yy + zz)) * scale.x;
    m[0][1] = (xy - wz) * scale.y;
    m[0][2] = (xz + wy) * scale.z;
    m[0][3] = position.x;

    m[1][0] = (xy + wz) * scale.x;
    m[1][1] = (1.0 - (xx + zz)) * scale.y;
    m[1][2] = (yz - wx) * scale.z;
    m[1][3] = position.y;

    m[2][0] = (xz - wy) * scale.x;
    m[2][1] = (yz + wx) * scale.y;
    m[2][2] = (1.0 - (xx + yy)) * scale.z;
    m[2][3] = position.z;

    m[3][0] = 0.0;
    m[3][1] = 0.0;
    m[3][2] = 0.0;
    m[3][3] = 1.0;

    return m;
}

float4x4 TRS_To_Inverse_Matrix(float3 position, float4 rotation, float3 scale)
{
    float3 is = 1.0 / (scale + 1e-6);

    float x2 = rotation.x + rotation.x;
    float y2 = rotation.y + rotation.y;
    float z2 = rotation.z + rotation.z;
    float xx = rotation.x * x2;
    float xy = rotation.x * y2;
    float xz = rotation.x * z2;
    float yy = rotation.y * y2;
    float yz = rotation.y * z2;
    float zz = rotation.z * z2;
    float wx = rotation.w * x2;
    float wy = rotation.w * y2;
    float wz = rotation.w * z2;

    float3 r0 = float3(1.0 - (yy + zz), xy + wz, xz - wy);
    float3 r1 = float3(xy - wz, 1.0 - (xx + zz), yz + wx);
    float3 r2 = float3(xz + wy, yz - wx, 1.0 - (xx + yy));

    float4x4 m;

    m[0][0] = r0.x * is.x;
    m[0][1] = r1.x * is.x;
    m[0][2] = r2.x * is.x;
    
    m[1][0] = r0.y * is.y;
    m[1][1] = r1.y * is.y;
    m[1][2] = r2.y * is.y;

    m[2][0] = r0.z * is.z;
    m[2][1] = r1.z * is.z;
    m[2][2] = r2.z * is.z;

    float3 negPos = -position;
    m[0][3] = m[0][0] * negPos.x + m[0][1] * negPos.y + m[0][2] * negPos.z;
    m[1][3] = m[1][0] * negPos.x + m[1][1] * negPos.y + m[1][2] * negPos.z;
    m[2][3] = m[2][0] * negPos.x + m[2][1] * negPos.y + m[2][2] * negPos.z;

    m[3][0] = 0;
    m[3][1] = 0;
    m[3][2] = 0;
    m[3][3] = 1;

    return m;
}

#endif