#ifndef INDIRECT_INCLUDES
#define INDIRECT_INCLUDES

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    struct IndirectData
    {
        float4x4 objectToWorld;
        float4x4 worldToObject;
    };
    StructuredBuffer<IndirectData> _IndirectInstanceData;
#endif

void SetupIndirect()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    unity_ObjectToWorld = _IndirectInstanceData[unity_InstanceID].objectToWorld;
    unity_WorldToObject = _IndirectInstanceData[unity_InstanceID].worldToObject;
#endif
}

#endif