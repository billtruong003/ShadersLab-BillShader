#ifndef INDIRECT_INCLUDES
#define INDIRECT_INCLUDES

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct IndirectData {
    float4x4 objectToWorld;
    float4x4 worldToObject;
};

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
StructuredBuffer<IndirectData> _IndirectInstanceData;
#endif

void SetupIndirect() {
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    IndirectData data = _IndirectInstanceData[unity_InstanceID];
    unity_ObjectToWorld = data.objectToWorld;
    unity_WorldToObject = data.worldToObject;
#endif
}

#endif