#ifndef FOLIAGE_INPUT_INCLUDES
#define FOLIAGE_INPUT_INCLUDES

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _TopColor;
    half4 _BottomColor;
    half4 _ShadowColor;
    half4 _TranslucencyColor;
    half4 _EmissionColor;
    half4 _DetailColor;
    
    half _Cutoff;
    half _GroundBlend;
    half _DetailScale;
    half _DetailBlend;
    
    half _TranslucencyGain;
    half _TranslucencyDistortion;
    half _TranslucencyPower;
    
    half _WindSpeed;
    half _WindStrength;
    half _WindFrequency;
    
    half _InteractionRadius;
    half _InteractionStrength;
    half _Softness;
    half _SSAONormalFlatten;
CBUFFER_END

float3 _GlobalInteractorPos;
TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_AlphaMask);
TEXTURE2D(_DetailMap); SAMPLER(sampler_DetailMap);

float3 ApplyWindAndInteraction(float3 positionWS, float2 uv)
{
    float wind = sin(_Time.y * _WindSpeed + positionWS.x + positionWS.z) * _WindStrength * uv.y;
    positionWS.x += wind;
    positionWS.z += wind;

    float3 dir = positionWS - _GlobalInteractorPos;
    dir.y = 0;
    float influence = saturate(1.0 - length(dir) / _InteractionRadius);
    float3 push = normalize(dir) * influence * influence * _InteractionStrength * uv.y;
    
    positionWS += push;
    positionWS.y -= length(push) * 0.5;
    
    return positionWS;
}

half3 CalculateTranslucency(half3 lightDir, half3 normal, half3 viewDir)
{
    half3 backLitDir = lightDir + (normal * _TranslucencyDistortion);
    half transDot = saturate(dot(viewDir, -backLitDir));
    return _TranslucencyGain * pow(transDot, _TranslucencyPower) * _TranslucencyColor.rgb;
}

#endif