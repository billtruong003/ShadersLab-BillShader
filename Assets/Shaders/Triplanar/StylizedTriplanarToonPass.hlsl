#ifndef STYLIZED_TRIPLANAR_LOGIC_INCLUDED
#define STYLIZED_TRIPLANAR_LOGIC_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
    float2 lightmapUV   : TEXCOORD1;
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float2 uv           : TEXCOORD2;
    float2 lightmapUV   : TEXCOORD3;
    
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        float4 shadowCoord : TEXCOORD4;
    #endif
};

CBUFFER_START(UnityPerMaterial)
    sampler2D _BaseMap;
    float4 _BaseMap_ST;
    float4 _BaseColor;

    sampler2D _NormalMap;
    float _NormalScale;

    sampler2D _ToonRamp;
    float _ToonRampContribution;
    
    float _MapScale;
    float _BlendSharpness;

    float _CelShadingThreshold;
    float _CelShadingSmoothness;

    float _SpecularThreshold;
    float _SpecularSmoothness;
    float _SpecularIntensity;
CBUFFER_END

float3 CalculateTriplanarWeights(float3 normalWS, float sharpness)
{
    float3 weights = abs(normalWS);
    weights = pow(weights, sharpness);
    return weights / (weights.x + weights.y + weights.z);
}

float4 SampleTriplanarTexture(sampler2D tex, float3 positionWS, float3 normalWS, float scale, float sharpness)
{
    float3 scaledPosition = positionWS * scale;
    
    float2 uvX = scaledPosition.yz;
    float2 uvY = scaledPosition.xz;
    float2 uvZ = scaledPosition.xy;

    float4 sampleX = tex2D(tex, uvX);
    float4 sampleY = tex2D(tex, uvY);
    float4 sampleZ = tex2D(tex, uvZ);

    float3 weights = CalculateTriplanarWeights(normalWS, sharpness);
    
    return sampleX * weights.x + sampleY * weights.y + sampleZ * weights.z;
}

float3 SampleTriplanarNormal(sampler2D tex, float3 positionWS, float3 normalWS, float scale, float sharpness, float normalScale)
{
    float3 scaledPosition = positionWS * scale;
    
    float2 uvX = scaledPosition.yz;
    float2 uvY = scaledPosition.xz;
    float2 uvZ = scaledPosition.xy;

    float3 tangentNormalX = UnpackNormalScale(tex2D(tex, uvX), normalScale);
    float3 tangentNormalY = UnpackNormalScale(tex2D(tex, uvY), normalScale);
    float3 tangentNormalZ = UnpackNormalScale(tex2D(tex, uvZ), normalScale);

    float3 worldNormalX = float3(0, tangentNormalX.yx);
    float3 worldNormalY = float3(tangentNormalY.x, 0, tangentNormalY.y);
    float3 worldNormalZ = float3(tangentNormalZ.xy, 0);

    float3 weights = CalculateTriplanarWeights(normalWS, sharpness);

    float3 blendedNormal = worldNormalX * weights.x + worldNormalY * weights.y + worldNormalZ * weights.z;
    
    return normalize(normalWS + blendedNormal);
}

Varyings MainVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    output.lightmapUV = input.lightmapUV.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        output.shadowCoord = GetShadowCoord(input);
    #endif

    return output;
}

float4 MainFragment(Varyings input) : SV_Target
{
    float3 normalWS = normalize(input.normalWS);
    
    float4 albedo = SampleTriplanarTexture(_BaseMap, input.positionWS, normalWS, _MapScale, _BlendSharpness) * _BaseColor;
    float3 sampledNormal = SampleTriplanarNormal(_NormalMap, input.positionWS, normalWS, _MapScale, _BlendSharpness, _NormalScale);
    normalWS = normalize(sampledNormal);
    
    float3 viewDirectionWS = GetWorldSpaceViewDir(input.positionWS);
    Light mainLight = GetMainLight(input.shadowCoord);
    
    float NdotL = saturate(dot(normalWS, mainLight.direction));

    float rampValue = tex2D(_ToonRamp, float2(NdotL, 0.5)).r;
    float celShading = smoothstep(_CelShadingThreshold - _CelShadingSmoothness, _CelShadingThreshold + _CelShadingSmoothness, rampValue);
    celShading = lerp(celShading, rampValue, _ToonRampContribution);

    float3 lightContribution = mainLight.color * celShading;
    
    float3 halfwayVector = SafeNormalize(mainLight.direction + viewDirectionWS);
    float NdotH = saturate(dot(normalWS, halfwayVector));
    float specularTerm = pow(NdotH, 128);
    float specularMask = smoothstep(_SpecularThreshold - _SpecularSmoothness, _SpecularThreshold + _SpecularSmoothness, specularTerm);
    
    float3 specularContribution = mainLight.color * specularMask * _SpecularIntensity;

    float3 gi = SampleGI(input.lightmapUV, input.positionWS, normalWS);

    float3 finalColor = albedo.rgb * (lightContribution + gi) + specularContribution;
    
    return float4(finalColor, albedo.a);
}

// Shadow Caster Pass
Varyings ShadowCasterVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToShadowHClip(positionWS);
    return output;
}

float4 ShadowCasterFragment(Varyings input) : SV_Target
{
    return 0;
}

// Meta Pass (for Light Baking)
Varyings MetaVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    output.positionCS = UnityMetaVertexPosition(input.positionOS, input.lightmapUV, 0, unity_LightmapST, false);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
    return output;
}

float4 MetaFragment(Varyings input) : SV_Target
{
    float3 normalWS = normalize(input.normalWS);
    float4 albedo = SampleTriplanarTexture(_BaseMap, input.positionWS, normalWS, _MapScale, _BlendSharpness) * _BaseColor;

    UnityMetaInput metaInput;
    metaInput.Albedo = albedo.rgb;
    metaInput.Emission = 0;
    
    return UnityMetaFragment(metaInput);
}

#endif