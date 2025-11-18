#ifndef TOON_LIT_CORE_INCLUDED
#define TOON_LIT_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"

// ====================================================================
// Structs
// ====================================================================
struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 tangentOS  : TANGENT;
    float2 uv         : TEXCOORD0;
    float2 uvLM       : TEXCOORD1; // Lightmap UVs (Meta pass cần)
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS  : SV_POSITION;
    float3 positionWS  : TEXCOORD0;
    float3 normalWS    : TEXCOORD1;
    float2 uv          : TEXCOORD2;
#if defined(_NORMALMAP_ON)
    float4 tangentWS   : TEXCOORD3;   // xyz: tangent, w: bitangent sign (hoặc bitangentWS trực tiếp)
#endif
    UNITY_VERTEX_OUTPUT_STEREO
};

struct VaryingsShadow
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
};

// ====================================================================
// Material Properties
// ====================================================================
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;
    half   _BumpScale;
    half   _Cutoff;

    half4 _EmissionColor;

    half4 _FakeLightColor;
    float3 _FakeLightDirection;

    half4 _AmbientColor;
    half  _MaxBrightness;

    half4 _ShadowTint;
    half4 _MidtoneColor;
    half  _ShadowThreshold;
    half  _MidtoneThreshold;
    half  _ToonRampSmoothness;

    half4 _AddLightShadowTint;
    half4 _AddLightMidtoneColor;
    half  _AddLightShadowThreshold;
    half  _AddLightMidtoneThreshold;
    half  _AddLightRampSmoothness;

    half _IndirectSpecularIntensity;
CBUFFER_END

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);

// ====================================================================
// Helper Functions
// ====================================================================
half4 GetAlbedoAndAlpha(float2 uv)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
}

void ApplyAlphaClip(half alpha)
{
#if defined(_ALPHACLIP_ON)
    clip(alpha - _Cutoff);
#endif
}

half3 ApplyEmission(half3 color, float2 uv)
{
#if defined(_EMISSION_ON)
    half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
    color += emission;
#endif
    return color;
}

float3 ApplyNormalMap(float2 uv, float3 normalWS, float3 tangentWS, float3 bitangentWS)
{
    float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
    float3x3 TBN = float3x3(tangentWS, bitangentWS, normalWS);
    return normalize(mul(normalTS, TBN));
}

// ====================================================================
// Indirect Lighting (SH + Reflection)
// ====================================================================
struct IndirectLighting
{
    half3 diffuse;
    half3 specular;
};

IndirectLighting SampleIndirectLighting(float3 positionWS, float3 normalWS, float3 viewDirWS, float4 positionCS)
{
    IndirectLighting indirect;
    indirect.diffuse  = 0;
    indirect.specular = 0;

    // Diffuse GI
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    EvaluateAdaptiveProbeVolume(positionWS, normalWS, viewDirWS, positionCS.xy / positionCS.w, GetMeshRenderingLayer(), indirect.diffuse);
#else
    indirect.diffuse = SampleSH(normalWS);
#endif

    // Specular reflection (environment)
#if defined(_INDIRECTSPECULAR_ON)
    float3 reflectVec = reflect(-viewDirWS, normalWS);
    half perceptualRoughness = 0.0h; // Toon nên dùng rough = 0 (mirror-like)
    indirect.specular = GlossyEnvironmentReflection(reflectVec, positionWS, perceptualRoughness, 1.0h) * _IndirectSpecularIntensity;
#endif

    return indirect;
}

// ====================================================================
// Fake Light Fallback
// ====================================================================
Light GetEffectiveMainLight(float3 positionWS)
{
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));

#if defined(_FORCE_FAKELIGHT_ON)
    mainLight.direction = normalize(_FakeLightDirection);
    mainLight.color     = _FakeLightColor.rgb;
    mainLight.shadowAttenuation = 1.0;
    mainLight.distanceAttenuation = 1.0;
#elif defined(_FAKELIGHT_ON)
    bool hasRealLight = any(mainLight.color > 0.001);
    if (!hasRealLight)
    {
        mainLight.direction = normalize(_FakeLightDirection);
        mainLight.color     = _FakeLightColor.rgb;
        mainLight.shadowAttenuation = 1.0;
        mainLight.distanceAttenuation = 1.0;
    }
#endif
    return mainLight;
}

// ====================================================================
// Toon Ramp
// ====================================================================
half3 ApplyConfigurableToonRamp(half NdotL, half3 lightColor,
                                half3 shadowTint, half3 midtoneColor,
                                half shadowThreshold, half midtoneThreshold, half smoothness)
{
#if defined(_TOON_STYLE_HARD)
    half shadowFactor  = step(shadowThreshold, NdotL);
    half midtoneFactor = step(midtoneThreshold, NdotL);
#else
    half shadowFactor  = smoothstep(shadowThreshold, shadowThreshold + smoothness, NdotL);
    half midtoneFactor = smoothstep(midtoneThreshold, midtoneThreshold + smoothness, NdotL);
#endif

    half3 ramp = lerp(shadowTint, midtoneColor, shadowFactor);
    ramp = lerp(ramp, lightColor, midtoneFactor);
    return ramp;
}

// ====================================================================
// Main Toon Lighting Calculation
// ====================================================================
half3 CalculateToonLighting(float3 normalWS, float3 positionWS, Light mainLight)
{
    half NdotL = dot(normalWS, mainLight.direction) * 0.5h + 0.5h;

    // Main light toon ramp
    half3 mainRamp = ApplyConfigurableToonRamp(NdotL,
        mainLight.color,
        _ShadowTint.rgb,
        _MidtoneColor.rgb,
        _ShadowThreshold,
        _MidtoneThreshold,
        _ToonRampSmoothness);

    half3 lighting = mainRamp * mainLight.shadowAttenuation;

    // Additional lights
#if defined(_ADDITIONAL_LIGHTS)
    uint additionalLightsCount = GetAdditionalLightsCount();
    for (uint i = 0u; i < additionalLightsCount; ++i)
    {
        Light addLight = GetAdditionalLight(i, positionWS);
        half addNdotL = dot(normalWS, addLight.direction) * 0.5h + 0.5h;

        half3 addRamp = ApplyConfigurableToonRamp(addNdotL,
            addLight.color,
            _AddLightShadowTint.rgb,
            _AddLightMidtoneColor.rgb,
            _AddLightShadowThreshold,
            _AddLightMidtoneThreshold,
            _AddLightRampSmoothness);

        lighting += addRamp * addLight.distanceAttenuation * addLight.shadowAttenuation;
    }
#endif

    return min(lighting, _MaxBrightness);
}

#endif // TOON_LIT_CORE_INCLUDED