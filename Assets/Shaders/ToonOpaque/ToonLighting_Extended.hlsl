#ifndef TOON_LIGHTING_EXTENDED_INCLUDED
#define TOON_LIGHTING_EXTENDED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap);
TEXTURE2D(_EmissionMap);
TEXTURE2D(_ToonRamp);       SAMPLER(sampler_ToonRamp);
TEXTURE2D(_MatCapTex);      SAMPLER(sampler_MatCapTex);

TEXTURE2D(_DissolveMap);    SAMPLER(sampler_DissolveMap);

TEXTURE2D(_MaskControlMap); SAMPLER(sampler_MaskControlMap);
TEXTURE2D(_Layer1Tex);      SAMPLER(sampler_Layer1Tex);
TEXTURE2D(_Layer2Tex);      SAMPLER(sampler_Layer2Tex);
TEXTURE2D(_Layer3Tex);      SAMPLER(sampler_Layer3Tex);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _BaseMap_ST;
    float4 _EmissionColor;
    float4 _ShadowColor;
    float4 _RimColor;
    float4 _SpecularColor;
    float4 _OutlineColor;
    float _OutlineWidth;
    float _Cutoff;
    float _RampThreshold;
    float _RampSmoothness;
    float _SpecularSize;
    float _SpecularFalloff;
    float _RimPower;
    float _RimThreshold;
    float _MatCapStrength;
    float _BumpScale;

    float _DissolveAmount;
    float _DissolveEdgeWidth;
    float4 _DissolveEdgeColor;
    float _DissolveScale;

    float4 _Layer1Color;
    float4 _Layer2Color;
    float4 _Layer3Color;
    float _TriplanarScale;
    float _TriplanarBlendSharpness;
CBUFFER_END

struct ToonSurfaceData
{
    half3 albedo;
    half alpha;
    half3 emission;
    half3 normalWS;
    half3 viewDirWS;
    half3 positionWS;
};

half3 SampleToonRamp(float NdotL)
{
    float rampU = NdotL * 0.5 + 0.5;
    return SAMPLE_TEXTURE2D(_ToonRamp, sampler_ToonRamp, float2(rampU, 0.5)).rgb;
}

half3 CalculateCrispSpecular(Light light, half3 normalWS, half3 viewDirWS)
{
    float3 halfVec = SafeNormalize(light.direction + viewDirWS);
    float NdotH = saturate(dot(normalWS, halfVec));
    float spec = smoothstep(1.0 - _SpecularSize, 1.0 - _SpecularSize + _SpecularFalloff, NdotH);
    return spec * _SpecularColor.rgb * light.color * light.shadowAttenuation;
}

half3 CalculateToonLight(Light light, ToonSurfaceData surfaceData)
{
    float NdotL = dot(surfaceData.normalWS, light.direction);
    float lightIntensity = smoothstep(_RampThreshold, _RampThreshold + _RampSmoothness, NdotL);
    float shadowAtten = light.shadowAttenuation * light.distanceAttenuation;
    
    half3 shadowColor = lerp(_ShadowColor.rgb, surfaceData.albedo, 0.5);
    half3 lightColor = lerp(shadowColor, surfaceData.albedo * light.color, lightIntensity * shadowAtten);
    
    half3 specular = CalculateCrispSpecular(light, surfaceData.normalWS, surfaceData.viewDirWS);
    return lightColor + specular;
}

half3 CalculateRimLight(half3 normalWS, half3 viewDirWS)
{
    float NdotV = 1.0 - saturate(dot(normalWS, viewDirWS));
    float rim = smoothstep(_RimThreshold, _RimThreshold + 0.01, pow(NdotV, _RimPower));
    return rim * _RimColor.rgb * _RimColor.a;
}

half3 ApplyMatCap(half3 viewDirWS, half3 normalWS)
{
    float3 viewNormal = mul((float3x3)GetWorldToViewMatrix(), normalWS);
    float2 matCapUV = viewNormal.xy * 0.5 + 0.5;
    half3 matCap = SAMPLE_TEXTURE2D(_MatCapTex, sampler_MatCapTex, matCapUV).rgb;
    return matCap * _MatCapStrength;
}

void ApplyDissolve(float2 uv, inout half3 color, inout half alpha)
{
    float noise = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, uv * _DissolveScale).r;
    float cut = _DissolveAmount;
    clip(noise - cut);

    float edge = smoothstep(cut, cut + _DissolveEdgeWidth, noise);
    float3 edgeColor = _DissolveEdgeColor.rgb * _DissolveEdgeColor.a;
    color = lerp(edgeColor, color, edge);
}

// --- FIX: Changed return type and UV variable types ---
half4 SampleTriplanar(TEXTURE2D(tex), SAMPLER(samp), float3 positionWS, float3 normalWS, float scale)
{
    // FIXED: Used float2 instead of float3 for UV coordinates
    float2 uvX = positionWS.zy * scale;
    float2 uvY = positionWS.xz * scale;
    float2 uvZ = positionWS.xy * scale;

    half4 colX = SAMPLE_TEXTURE2D(tex, samp, uvX);
    half4 colY = SAMPLE_TEXTURE2D(tex, samp, uvY);
    half4 colZ = SAMPLE_TEXTURE2D(tex, samp, uvZ);

    float3 blend = pow(abs(normalWS), _TriplanarBlendSharpness);
    blend /= (blend.x + blend.y + blend.z + 0.0001); // Normalize blend weights safely

    return colX * blend.x + colY * blend.y + colZ * blend.z;
}

half3 ApplyMultiLayerMasking(float2 uv, float3 positionWS, float3 normalWS, half3 baseAlbedo)
{
    half4 maskControl = SAMPLE_TEXTURE2D(_MaskControlMap, sampler_MaskControlMap, uv);
    
    half3 layer1, layer2, layer3;

    #if defined(_TRIPLANAR_MASK)
        layer1 = SampleTriplanar(_Layer1Tex, sampler_Layer1Tex, positionWS, normalWS, _TriplanarScale).rgb * _Layer1Color.rgb;
        layer2 = SampleTriplanar(_Layer2Tex, sampler_Layer2Tex, positionWS, normalWS, _TriplanarScale).rgb * _Layer2Color.rgb;
        layer3 = SampleTriplanar(_Layer3Tex, sampler_Layer3Tex, positionWS, normalWS, _TriplanarScale).rgb * _Layer3Color.rgb;
    #else
        layer1 = SAMPLE_TEXTURE2D(_Layer1Tex, sampler_Layer1Tex, uv).rgb * _Layer1Color.rgb;
        layer2 = SAMPLE_TEXTURE2D(_Layer2Tex, sampler_Layer2Tex, uv).rgb * _Layer2Color.rgb;
        layer3 = SAMPLE_TEXTURE2D(_Layer3Tex, sampler_Layer3Tex, uv).rgb * _Layer3Color.rgb;
    #endif

    half3 result = baseAlbedo;
    result = lerp(result, layer1, maskControl.r);
    result = lerp(result, layer2, maskControl.g);
    result = lerp(result, layer3, maskControl.b);
    
    return result;
}

#endif