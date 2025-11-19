#ifndef CUSTOM_TRIPLANAR_CORE_INCLUDED
#define CUSTOM_TRIPLANAR_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Structs to pass surface data
struct SurfaceData {
    half3 albedo;
    half3 normalWS;
    half3 emission;
    half smoothness;
};

// All properties are defined here once
CBUFFER_START(UnityPerMaterial)
half4 _Color, _Tint, _AmbientColor;
half _Scale, _SideScale, _NoiseScale;
half _TopSpread, _EdgeWidth;
half _RimPower;
half4 _RimColor, _RimColor2;
half _NormalStrength;
half4 _SpecColor;
half _Smoothness;
CBUFFER_END

// All textures are defined here once
Texture2D _MainTex;     SamplerState sampler_MainTex;
Texture2D _NormalT;     SamplerState sampler_NormalT;
Texture2D _MainTexSide; SamplerState sampler_MainTexSide;
Texture2D _Normal;      SamplerState sampler_Normal;
Texture2D _Noise;       SamplerState sampler_Noise;

half3 UnpackNormalFromTexture_Triplanar(half4 packedNormal) {
#if defined(UNITY_NO_DXT5nm)
    return packedNormal.xyz * 2.0 - 1.0;
#else
    half3 normal;
    normal.xy = packedNormal.wy * 2.0 - 1.0;
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    return normal;
#endif
}

// Reusable function to calculate a blended triplanar normal
void CalculateTriplanarNormal(float3 worldPos, float3 worldNormal, float3 blendWeights, out half3 triplanarNormal,
    Texture2D normalMap, SamplerState ss, half texScale) {
    half3 nX = UnpackNormalFromTexture_Triplanar(SAMPLE_TEXTURE2D(normalMap, ss, worldPos.zy * texScale));
    half3 nY = UnpackNormalFromTexture_Triplanar(SAMPLE_TEXTURE2D(normalMap, ss, worldPos.xz * texScale));
    half3 nZ = UnpackNormalFromTexture_Triplanar(SAMPLE_TEXTURE2D(normalMap, ss, worldPos.xy * texScale));

    // Tangent space normals from texture are blended
    half3 blendedNormalTex = nX * blendWeights.x + nY * blendWeights.y + nZ * blendWeights.z;

    // Reorient the blended normal to world space
    triplanarNormal = normalize(half3(worldNormal.z, worldNormal.x, worldNormal.y) * blendedNormalTex.x +
        half3(worldNormal.y, worldNormal.z, worldNormal.x) * blendedNormalTex.y +
        worldNormal * blendedNormalTex.z);

    triplanarNormal = normalize(lerp(worldNormal, triplanarNormal, _NormalStrength));
}

// THE CORE FUNCTION: All passes call this to get surface properties
void GetSurfaceData(float3 worldPos, float3 worldNormal, float3 viewDir, out SurfaceData surface) {
    // Use absolute value for weights to prevent negative results on inverted normals
    half3 blendWeights = saturate(pow(abs(worldNormal), 4));
    blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z);

    // --- Noise Calculation for Blending
    half3 noiseTexture = lerp(lerp(SAMPLE_TEXTURE2D(_Noise, sampler_Noise, worldPos.xy * _NoiseScale).rgb, SAMPLE_TEXTURE2D(_Noise, sampler_Noise, worldPos.zy * _NoiseScale).rgb, blendWeights.x), SAMPLE_TEXTURE2D(_Noise, sampler_Noise, worldPos.xz * _NoiseScale).rgb, blendWeights.y);
    half noiseOffset = noiseTexture.g + (noiseTexture.r + noiseTexture.b) * 0.5 - 0.5;
    half worldNormalDotNoise = worldNormal.y + noiseOffset;

    // --- Blend Factor using smoothstep for soft edges
    half blendFactor = smoothstep(_TopSpread, _TopSpread + _EdgeWidth, worldNormalDotNoise);

    // --- Albedo Triplanar Mapping
    half3 topTex = lerp(lerp(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, worldPos.xy * _Scale).rgb, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, worldPos.zy * _Scale).rgb, blendWeights.x), SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, worldPos.zx * _Scale).rgb, blendWeights.y);
    half3 sideTex = lerp(lerp(SAMPLE_TEXTURE2D(_MainTexSide, sampler_MainTexSide, worldPos.xy * _SideScale).rgb, SAMPLE_TEXTURE2D(_MainTexSide, sampler_MainTexSide, worldPos.zy * _SideScale).rgb, blendWeights.x), SAMPLE_TEXTURE2D(_MainTexSide, sampler_MainTexSide, worldPos.zx * _SideScale).rgb, blendWeights.y);

    // --- Normal Map Triplanar Mapping & Blending
    half3 topNormalWS, sideNormalWS;
    CalculateTriplanarNormal(worldPos, worldNormal, blendWeights, topNormalWS, _NormalT, sampler_NormalT, _Scale);
    CalculateTriplanarNormal(worldPos, worldNormal, blendWeights, sideNormalWS, _Normal, sampler_Normal, _SideScale);

    // --- Final Surface Property Calculation
    surface.normalWS = normalize(lerp(sideNormalWS, topNormalWS, blendFactor));
    surface.albedo = lerp(sideTex, topTex, blendFactor) * _Color.rgb;
    surface.smoothness = _Smoothness;

    // --- Rim Light / Emission
    half rim = 1.0 - saturate(dot(viewDir, surface.normalWS));
    half3 rimColor = lerp(_RimColor2.rgb, _RimColor.rgb, blendFactor);
    surface.emission = pow(rim, _RimPower) * rimColor;
}

#endif // CUSTOM_TRIPLANAR_CORE_INCLUDED