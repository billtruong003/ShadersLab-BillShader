Shader "CleanCode/URP/Toon Snow Ice (Single File)"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseMap("Albedo (RGB)", 2D) = "white"{}
        [HDR] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump"{}
        _BumpScale("Normal Intensity", Range(0.0, 2.0)) = 1.0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Header(Toon Shading)]
        _ShadowTint("Shadow Tint", Color) = (0.1, 0.1, 0.2, 1.0)
        _MidtoneColor("Mid-tone Color", Color) = (0.6, 0.6, 0.6, 1.0)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.4
        _MidtoneThreshold("Mid-tone Threshold", Range(0, 1)) = 0.8
        _ToonRampSmoothness("Ramp Smoothness", Range(0.001, 0.5)) = 0.05
        _MaxBrightness("Max Brightness", Range(0, 5)) = 1.5
        _AmbientColor("Ambient Color", Color) = (0.5, 0.5, 0.5, 0)

        [Header(Snow Buildup)]
        _SnowUpVector("Snow Direction (Object Space)", Vector) = (0, 1, 0, 0)
        _SnowCoverage("Snow Coverage", Range(-1, 1)) = 0.5
        _SnowCoverageSoftness("Snow Transition Softness", Range(0.01, 1.0)) = 0.2
        _SnowMaxDisplacement("Snow Buildup Height", Range(0, 0.5)) = 0.1

        [Header(Snow and Ice Material)]
        [HDR] _SnowBaseColor("Snow Base Color", Color) = (0.8, 0.9, 1.0, 1.0)
        [HDR] _SnowTopColor("Snow Top Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _SnowBlendMinHeight("Snow Color Blend Min Height", Float) = 0
        _SnowBlendMaxHeight("Snow Color Blend Max Height", Float) = 2

        [Header(Ice Effects)]
        [HDR] _IceSpecularColor("Ice Specular Color", Color) = (0.9, 1.0, 1.0, 1)
        _IceSpecularThreshold("Ice Specular Threshold", Range(0, 1)) = 0.7
        _IceSpecularSmoothness("Ice Specular Smoothness", Range(0.01, 0.5)) = 0.1

        [HDR] _IceRimColor("Ice Rim Color", Color) = (0.5, 0.8, 1.0, 1)
        _IceRimPower("Ice Rim Power", Range(0.1, 10)) = 3.0

        [HDR] _IceTranslucencyColor("Ice Translucency Color", Color) = (0.7, 0.9, 0.3, 1)
        _IceTranslucencyStrength("Ice Translucency Strength", Range(0, 5)) = 1.0

        [Header(Render States)]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Culling Mode", Float) = 2
        [Enum(Opaque, 0, Cutout, 1)] _RenderMode ("Render Mode", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull [_CullMode]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _NORMALMAP_ON
            #pragma shader_feature_local_fragment _ALPHACLIP_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float snowFactor : TEXCOORD3;

                #if defined(_NORMALMAP_ON)
                    float3 tangentWS : TEXCOORD4;
                    float3 bitangentWS : TEXCOORD5;
                #endif
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float _BumpScale;
            float _Cutoff;

            float4 _ShadowTint;
            float4 _MidtoneColor;
            float _ShadowThreshold;
            float _MidtoneThreshold;
            float _ToonRampSmoothness;
            float _MaxBrightness;
            float4 _AmbientColor;

            float3 _SnowUpVector;
            float _SnowCoverage;
            float _SnowCoverageSoftness;
            float _SnowMaxDisplacement;

            float4 _SnowBaseColor;
            float4 _SnowTopColor;
            float _SnowBlendMinHeight;
            float _SnowBlendMaxHeight;

            float4 _IceSpecularColor;
            float _IceSpecularThreshold;
            float _IceSpecularSmoothness;
            float4 _IceRimColor;
            float _IceRimPower;
            float3 _IceTranslucencyColor;
            float _IceTranslucencyStrength;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            half3 ApplyToonRamp(float NdotL, float3 lightColor, float3 shadowTint, float3 midtoneColor, float shadowThreshold, float midtoneThreshold, float smoothness)
            {
                half shadowFactor = smoothstep(shadowThreshold - smoothness, shadowThreshold + smoothness, NdotL);
                half midtoneFactor = smoothstep(midtoneThreshold - smoothness, midtoneThreshold + smoothness, NdotL);

                half3 rampedLight = lerp(shadowTint, midtoneColor, shadowFactor);
                rampedLight = lerp(rampedLight, lightColor, midtoneFactor);

                return rampedLight;
            }

            half3 CalculateAdditionalLightsContribution(float3 normalWS, float3 worldPos)
            {
                half3 additionalLightContribution = 0.0h;
                #ifdef _ADDITIONAL_LIGHTS
                    uint lightCount = GetAdditionalLightsCount();
                    for (uint i = 0u;
                    i < lightCount;
                    ++i)
                    {
                        Light additionalLight = GetAdditionalLight(i, worldPos);
                        float NdotL = dot(normalWS, additionalLight.direction) * 0.5 + 0.5;
                        half3 addLightRamp = ApplyToonRamp(NdotL, additionalLight.color, 0, 0.5, 0.2, 0.6, _ToonRampSmoothness * 2.0);
                        additionalLightContribution += addLightRamp * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
                    }
                #endif
                return additionalLightContribution;
            }

            half3 CalculateBaseToonLighting(float3 normalWS, float3 worldPos, Light mainLight)
            {
                float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
                half3 mainLightRamp = ApplyToonRamp(NdotL, mainLight.color, _ShadowTint.rgb, _MidtoneColor.rgb, _ShadowThreshold, _MidtoneThreshold, _ToonRampSmoothness);
                half3 mainLightContribution = mainLightRamp * mainLight.shadowAttenuation;

                half3 additionalLightContribution = CalculateAdditionalLightsContribution(normalWS, worldPos);
                half3 totalLighting = mainLightContribution + additionalLightContribution;
                return min(totalLighting, _MaxBrightness);
            }

            half3 CalculateSnowIceLighting(float3 normalWS, float3 worldPos, float3 viewDir, Light mainLight)
            {
                float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;

                float heightFactor = saturate((worldPos.y - _SnowBlendMinHeight) / (_SnowBlendMaxHeight - _SnowBlendMinHeight));
                half3 snowColor = lerp(_SnowBaseColor.rgb, _SnowTopColor.rgb, heightFactor);

                half3 toonLight = ApplyToonRamp(NdotL, 1, 0.5, 0.8, 0.4, 0.8, _ToonRampSmoothness);
                half3 baseLitSnow = snowColor * toonLight * mainLight.color * mainLight.shadowAttenuation;

                float3 halfVec = SafeNormalize(viewDir + mainLight.direction);
                float NdotH = saturate(dot(normalWS, halfVec));
                half specularRamp = smoothstep(_IceSpecularThreshold - _IceSpecularSmoothness, _IceSpecularThreshold + _IceSpecularSmoothness, NdotH);
                half3 specular = specularRamp * _IceSpecularColor.rgb * mainLight.color * mainLight.shadowAttenuation;

                float3 backLightDir = -mainLight.direction;
                float backNdotL = dot(normalWS, backLightDir) * 0.5 + 0.5;
                half3 translucency = pow(backNdotL, 2) * mainLight.color * _IceTranslucencyStrength * _IceTranslucencyColor * mainLight.shadowAttenuation;

                half NdotV = 1.0h - saturate(dot(normalWS, viewDir));
                half3 rim = pow(NdotV, _IceRimPower) * _IceRimColor.rgb;

                half3 additionalLights = CalculateAdditionalLightsContribution(normalWS, worldPos);

                half3 totalLighting = baseLitSnow + specular + translucency + rim + (additionalLights * snowColor);
                return min(totalLighting, _MaxBrightness);
            }

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;

                float snowDotProduct = dot(v.normalOS, normalize(_SnowUpVector));
                o.snowFactor = smoothstep(_SnowCoverage - _SnowCoverageSoftness, _SnowCoverage + _SnowCoverageSoftness, snowDotProduct);

                float3 positionOS = v.positionOS.xyz;
                positionOS += v.normalOS * _SnowMaxDisplacement * o.snowFactor;

                o.positionWS = TransformObjectToWorld(positionOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);

                #if defined(_NORMALMAP_ON)
                    o.tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                    o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangentOS.w;
                #endif

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 normalWS = i.normalWS;
                #if defined(_NORMALMAP_ON)
                    float3 unpackedNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv), _BumpScale);
                    float3x3 TBN = float3x3(i.tangentWS, i.bitangentWS, i.normalWS);
                    normalWS = normalize(mul(unpackedNormal, TBN));
                #endif

                float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                half4 albedoSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;

                #if defined(_ALPHACLIP_ON)
                    clip(albedoSample.a - _Cutoff);
                #endif

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half3 ambient = SampleSH(normalWS) + lerp(0, _AmbientColor.rgb, _AmbientColor.a);

                half3 baseMaterialLighting = CalculateBaseToonLighting(normalWS, i.positionWS, mainLight);
                half3 baseMaterialColor = albedoSample.rgb * (baseMaterialLighting + ambient);

                half3 snowIceMaterialLighting = CalculateSnowIceLighting(normalWS, i.positionWS, viewDir, mainLight);
                half3 snowIceMaterialColor = snowIceMaterialLighting + (albedoSample.rgb * ambient);

                half3 finalColor = lerp(baseMaterialColor, snowIceMaterialColor, i.snowFactor);

                return half4(finalColor, albedoSample.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ColorMask 0
            Cull [_CullMode]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #pragma shader_feature_local_fragment _ALPHACLIP_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float _Cutoff;
            float3 _SnowUpVector;
            float _SnowCoverage;
            float _SnowCoverageSoftness;
            float _SnowMaxDisplacement;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            ShadowVaryings ShadowVert(Attributes v)
            {
                ShadowVaryings o;

                float snowDotProduct = dot(v.normalOS, normalize(_SnowUpVector));
                float snowFactor = smoothstep(_SnowCoverage - _SnowCoverageSoftness, _SnowCoverage + _SnowCoverageSoftness, snowDotProduct);

                float3 positionOS = v.positionOS.xyz;
                positionOS += v.normalOS * _SnowMaxDisplacement * snowFactor;

                o.positionCS = GetShadowCoord(GetVertexPositionInputs(positionOS));
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 ShadowFrag(ShadowVaryings i) : SV_Target
            {
                #if defined(_ALPHACLIP_ON)
                    half4 albedoSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                    clip(albedoSample.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }
    CustomEditor "ShaderGraph.PBRMasterGUI"
}
