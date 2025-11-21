#ifndef BILLS_TOON_CORE_INCLUDED
#define BILLS_TOON_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
    float4 color        : COLOR;
    float4 tangentOS    : TANGENT;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float2 uv           : TEXCOORD2;
    float4 color        : COLOR;
    float4 screenPos    : TEXCOORD3;
    #if defined(_NORMALMAP_ON)
        float3 tangentWS    : TEXCOORD4;
        float3 bitangentWS  : TEXCOORD5;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct IndirectLighting
{
    half3 diffuse;
    half3 specular;
};

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseColor;
    float  _BumpScale;
    float  _Cutoff;
    float4 _EmissionColor;
    float4 _FakeLightColor;
    float3 _FakeLightDirection;
    float  _MaxBrightness;
    
    // Toon General
    float4 _ShadowTint;
    float4 _MidtoneColor;
    float  _ShadowThreshold;
    float  _MidtoneThreshold;
    float  _ToonRampSmoothness;
    float4 _AmbientColor;

    // Additional Lights
    float4 _AddLightShadowTint;
    float4 _AddLightMidtoneColor;
    float  _AddLightShadowThreshold;
    float  _AddLightMidtoneThreshold;
    float  _AddLightRampSmoothness;

    // Metallic & Specular & Rim
    float  _Brightness;
    float  _Offset;
    float  _HighlightOffset;
    float  _RimPower;
    float4 _SpecuColor;
    float4 _HiColor;
    float4 _RimColor; // Used by both Metallic and Opaque Rim Light

    // Foliage
    float3 _TranslucencyColor;
    float  _TranslucencyStrength;
    float  _WindSpeed;
    float  _WindAmplitude;
    float3 _WindDirection;
    float  _WindNoiseScale;
    float  _WindFadeStart;
    float  _WindFadeEnd;

    // Bling
    float4 _BlingColor;
    float  _BlingIntensity;
    float  _BlingScale;
    float  _BlingSpeed;
    float  _BlingThreshold;
    float  _BlingFresnelPower;

    // Effects
    float4 _FresnelOutlineColor;
    float  _FresnelOutlineWidth;
    float  _FresnelOutlinePower;
    float  _FresnelOutlineSharpness;
    float4 _GlintColor;
    float  _GlintScale;
    float  _GlintSpeed;
    float  _GlintThreshold;
    
    // Dither
    float  _DitherFadeStart;
    float  _DitherFadeEnd;
    float  _DitherScale;
    float4 _DitherEdgeColor;
    float  _DitherEdgeWidth;
    
    float _IndirectSpecularIntensity;
    float _Morph;
    
    // Mask
    float  _MaskDivisions;
    float  _MaskBlend;
    half4  _MaskColor0,  _MaskColor1,  _MaskColor2,  _MaskColor3;
    half4  _MaskColor4,  _MaskColor5,  _MaskColor6,  _MaskColor7;
    half4  _MaskColor8,  _MaskColor9,  _MaskColor10, _MaskColor11;
    half4  _MaskColor12, _MaskColor13, _MaskColor14, _MaskColor15;
CBUFFER_END

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_BaseMapB);       SAMPLER(sampler_BaseMapB);
TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
TEXTURE2D(_Ramp);           SAMPLER(sampler_Ramp);
TEXTURE2D(_DitherPatternTex); SAMPLER(sampler_DitherPatternTex);
TEXTURE2D(_NoiseTex);       SAMPLER(sampler_NoiseTex); 
TEXTURE2D(_WindNoiseTex);   SAMPLER(sampler_WindNoiseTex);
TEXTURE2D(_MaskTexture);    SAMPLER(sampler_MaskTexture);

#include "Includes/Toon/ToonUber_Functions.hlsl"

// --- Common Vertex/Fragment Utilities ---

#if defined(_NORMALMAP_ON)
float3 ApplyNormalMap(float2 uv, float3 normalWS, float3 tangentWS, float3 bitangentWS)
{
    float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
    float3x3 TBN = float3x3(tangentWS, bitangentWS, normalWS);
    return normalize(mul(normalTS, TBN));
}
#endif

IndirectLighting SampleIndirectLighting(float3 positionWS, float3 normalWS, float3 viewDir, float4 positionCS)
{
    IndirectLighting indirectLighting;
    indirectLighting.diffuse = 0.0h;
    indirectLighting.specular = 0.0h;
    
#if defined(PROBE_VOLUMES_L1)
    EvaluateAdaptiveProbeVolume(positionWS, normalWS, viewDir, positionCS.xy, GetMeshRenderingLayer(), indirectLighting.diffuse);
#else
    indirectLighting.diffuse = SampleSH(normalWS);
#endif

#if defined(_INDIRECTSPECULAR_ON) && !defined(_SURFACETYPE_METALLIC) && !defined(_SURFACETYPE_BLING)
    float3 reflectionVector = reflect(-viewDir, normalWS);
    float2 normalizedScreenUV = positionCS.xy / positionCS.w;
    indirectLighting.specular = GlossyEnvironmentReflection(reflectionVector, positionWS, 0.0h, 1.0h, normalizedScreenUV);
    indirectLighting.specular *= _IndirectSpecularIntensity;
#endif

    return indirectLighting;
}

half3 ApplyDitherFade(float4 screenPos)
{
    #if defined(_DITHERFADE_ON)
        float2 perspectiveCorrectedScreenPos = screenPos.xy / screenPos.w;
        float distance = screenPos.w;
        float fadeRange = max(0.001, _DitherFadeStart - _DitherFadeEnd);
        float fadeThreshold = saturate((distance - _DitherFadeEnd) / fadeRange);
        float2 ditherUV = perspectiveCorrectedScreenPos * _ScreenParams.xy / _DitherScale;
        float noise = SAMPLE_TEXTURE2D(_DitherPatternTex, sampler_DitherPatternTex, ditherUV).r;
        float clipValue = fadeThreshold - noise;
        clip(clipValue);
        float edgeFactor = 1.0 - smoothstep(0, _DitherEdgeWidth, clipValue);
        return edgeFactor * _DitherEdgeColor.rgb * _DitherEdgeColor.a;
    #endif
    return 0.0;
}

half4 GetAlbedoAndAlpha(float2 uv)
{
    half4 albedoA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
    #if defined(_MORPH_ON)
        half4 albedoB = SAMPLE_TEXTURE2D(_BaseMapB, sampler_BaseMapB, uv);
        return lerp(albedoA, albedoB, saturate(_Morph)) * _BaseColor;
    #else
        return albedoA * _BaseColor;
    #endif
}

void ApplyAlphaClip(float2 uv)
{
    #if defined(_ALPHACLIP_ON)
        half albedoAlpha = GetAlbedoAndAlpha(uv).a;
        clip(albedoAlpha - _Cutoff);
    #endif
}

half3 ApplyEmission(half3 surfaceColor, float2 uv)
{
    #if defined(_EMISSION_ON)
        surfaceColor += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
    #endif
    return surfaceColor;
}

half3 ApplyTextureMask(half3 surfaceColor, float2 uv)
{
    #if defined(_TEXTUREMASK_ON)
        float2 cellCoords = floor(uv * _MaskDivisions);
        int cellIndex = int(cellCoords.y * _MaskDivisions + cellCoords.x);
        // Safety clamp
        cellIndex = clamp(cellIndex, 0, 15);

        half4 colors[16] = {
            _MaskColor0,  _MaskColor1,  _MaskColor2,  _MaskColor3,
            _MaskColor4,  _MaskColor5,  _MaskColor6,  _MaskColor7,
            _MaskColor8,  _MaskColor9,  _MaskColor10, _MaskColor11,
            _MaskColor12, _MaskColor13, _MaskColor14, _MaskColor15
        };

        half4 maskColor = colors[cellIndex];
        half3 tintedColor = surfaceColor * maskColor.rgb;
        surfaceColor = lerp(surfaceColor, tintedColor, maskColor.a * _MaskBlend);
    #endif
    return surfaceColor;
}

half3 ApplyFresnelOutline(half3 surfaceColor, float3 normalWS, float3 viewDir, float3 worldPos)
{
    #if defined(_OUTLINEMODE_FRESNEL)
        float fresnelDot = dot(normalWS, viewDir);
        float fresnelTerm = 1.0 - saturate(fresnelDot);
        float fresnelPower = pow(fresnelTerm, _FresnelOutlinePower); // Use standard pow for consistency
        float screenSpaceDerivative = fwidth(fresnelPower);
        float edgeWidth = screenSpaceDerivative * _FresnelOutlineSharpness;
        float outlineFactor = smoothstep(1.0 - _FresnelOutlineWidth - edgeWidth, 1.0 - _FresnelOutlineWidth, fresnelPower);
        half3 finalOutlineColor = _FresnelOutlineColor.rgb;

        #if defined(_OUTLINEGLINT_ON)
            float glintFactor = CalculateGlintFactor(worldPos);
            finalOutlineColor = lerp(finalOutlineColor, _GlintColor.rgb, glintFactor);
        #endif

        surfaceColor = lerp(surfaceColor, finalOutlineColor, outlineFactor);
    #endif
    return surfaceColor;
}

Light GetEffectiveMainLight(float3 positionWS)
{
    float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    Light mainLight = GetMainLight(shadowCoord);
    
    #if defined(_FORCE_FAKELIGHT_ON)
        mainLight.direction = normalize(_FakeLightDirection.xyz);
        mainLight.color = _FakeLightColor.rgb;
        mainLight.shadowAttenuation = 1.0;
        mainLight.distanceAttenuation = 1.0;
    #elif defined(_FAKELIGHT_ON)
        // Fallback if main light intensity is near zero
        bool hasRealLight = dot(mainLight.color, mainLight.color) > 0.001;
        if (!hasRealLight)
        {
            mainLight.direction = normalize(_FakeLightDirection.xyz);
            mainLight.color = _FakeLightColor.rgb;
            mainLight.shadowAttenuation = 1.0;
            mainLight.distanceAttenuation = 1.0;
        }
    #endif
    return mainLight;
}

#endif