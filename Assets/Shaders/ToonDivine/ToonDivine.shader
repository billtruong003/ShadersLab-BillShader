
Shader "Stylized/Divine Toon URP 2025"
{
    Properties
    {
        [Header(Core Surface)]
        _MainTex ("Albedo Map (Grayscale/ID)", 2D) = "white"{}
        _ColorPalette ("Palette Atlas (For Grid > 2)", 2D) = "white"{}
        [Header(Zone Logic)]
        [IntRange] _GridResolution ("Grid Resolution (1, 2, 4)", Range(1, 4)) = 1
        _ZoneBlend ("Zone Blending (0=Hard, 1=Gradient)", Range(0, 1)) = 0

        [Header(Zone Colors 2x2)]
        _ZoneColor0 ("Zone 1 (Bot-Left / Base)", Color) = (1, 1, 1, 1)
        _ZoneColor1 ("Zone 2 (Bot-Right)", Color) = (1, 0.5, 0.5, 1)
        _ZoneColor2 ("Zone 3 (Top-Left)", Color) = (0.5, 1, 0.5, 1)
        _ZoneColor3 ("Zone 4 (Top-Right)", Color) = (0.5, 0.5, 1, 1)

        [Header(Shadows)]
        _ShadowColor ("Shadow Color", Color) = (0.3, 0.3, 0.5, 1)
        _ShadowReceiveThreshold ("Shadow Threshold", Range(0, 1)) = 0.5
        _ShadowReceiveSmoothness ("Shadow Smoothness", Range(0.001, 1)) = 0.05

        [Header(Lighting)]
        _AmbientColor ("Ambient Color (Alpha=Intensity)", Color) = (0.0, 0.0, 0.0, 1)
        [IntRange] _RampBands ("Ramp Bands", Range(1, 8)) = 2
        _RampOffset ("Ramp Offset", Range(-1, 1)) = 0.0
        _RampSmoothness ("Band Smoothness", Range(0.001, 0.5)) = 0.01

        [Header(Specular)]
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularSize ("Specular Size", Range(0.0, 1.0)) = 0.05
        _SpecularSmoothness ("Specular Smoothness", Range(0.001, 0.5)) = 0.02

        [Header(Rim)]
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimThreshold ("Rim Threshold", Range(0, 1)) = 0.5
        _RimSmoothness ("Rim Smoothness", Range(0.001, 1)) = 0.1
        _RimIntensity ("Rim Intensity", Range(0, 10)) = 1

        [Header(System)]
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
        [HideInInspector] _Surface("__surface", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }

        Cull [_Cull]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _ColorPalette_ST;

        float4 _ZoneColor0;
        float4 _ZoneColor1;
        float4 _ZoneColor2;
        float4 _ZoneColor3;
        float _GridResolution;
        float _ZoneBlend;

        float4 _ShadowColor;
        float4 _AmbientColor;

        float _RampBands;
        float _RampOffset;
        float _RampSmoothness;

        float _ShadowReceiveThreshold;
        float _ShadowReceiveSmoothness;

        float4 _SpecularColor;
        float _SpecularSize;
        float _SpecularSmoothness;

        float4 _RimColor;
        float _RimThreshold;
        float _RimSmoothness;
        float _RimIntensity;
        float _Cutoff;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_ColorPalette);
        SAMPLER(sampler_ColorPalette);
        ENDHLSL

        Pass
        {
            Name "UniversalForward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            float3 GetDynamicZoneColor(float2 uv)
            {
                if (_GridResolution <= 1.0) return _ZoneColor0.rgb;

                if (_GridResolution <= 2.0)
                {
                    float2 weight = uv;
                    float2 hardWeight = step(0.5, uv);
                    float2 finalWeight = lerp(hardWeight, weight, _ZoneBlend);

                    float3 bot = lerp(_ZoneColor0.rgb, _ZoneColor1.rgb, finalWeight.x);
                    float3 top = lerp(_ZoneColor2.rgb, _ZoneColor3.rgb, finalWeight.x);
                    return lerp(bot, top, finalWeight.y);
                }

                float2 paletteUV = uv;
                if (_ZoneBlend < 0.99)
                {
                    float2 stepped = (floor(uv * _GridResolution) + 0.5) / _GridResolution;
                    paletteUV = lerp(stepped, uv, _ZoneBlend);
                }
                return SAMPLE_TEXTURE2D(_ColorPalette, sampler_ColorPalette, paletteUV).rgb;
            }

            float CalculateCelRamp(float value, float bands, float offset, float smoothness)
            {
                float val = saturate(value * 0.5 + 0.5 + offset);
                float x = val * bands;
                float level = floor(x);
                float fractVal = frac(x);
                float smoothPart = smoothstep(0.5 - smoothness, 0.5 + smoothness, fractVal);
                return (level + smoothPart) / bands;
            }

            float CalculateSmartShadow(float shadowAtten, float threshold, float smoothness)
            {
                return smoothstep(threshold - smoothness, threshold + smoothness, shadowAtten);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(mainTex.a - _Cutoff);

                float3 zoneColor = GetDynamicZoneColor(input.uv);
                float3 albedo = mainTex.rgb * zoneColor;

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lightDir = mainLight.direction;
                float shadowAttenRaw = mainLight.shadowAttenuation;
                float shadowMask = CalculateSmartShadow(shadowAttenRaw, _ShadowReceiveThreshold, _ShadowReceiveSmoothness);

                float ndotl = dot(normalWS, lightDir);

                float faceShadow = mainTex.a;
                float faceShadowFactor = lerp(-1.0, 1.0, faceShadow);
                float litNdotL = min(ndotl, faceShadowFactor);

                float ramp = CalculateCelRamp(litNdotL, _RampBands, _RampOffset, _RampSmoothness);
                ramp *= shadowMask;

                float3 diffuse = lerp(_ShadowColor.rgb, float3(1, 1, 1), ramp);
                diffuse *= mainLight.color;

                float3 ambient = _AmbientColor.rgb * _AmbientColor.a;

                float3 halfDir = normalize(lightDir + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specThreshold = 1.0 - _SpecularSize;
                float spec = smoothstep(specThreshold, specThreshold + _SpecularSmoothness, NdotH);
                float litMask = saturate(ndotl * 10.0);
                spec *= shadowMask * litMask;
                float3 specular = spec * _SpecularColor.rgb * mainLight.color;

                float3 finalColor = albedo * (diffuse + ambient) + specular;

                int pixelLightCount = GetAdditionalLightsCount();
                for (int i = 0;
                i < pixelLightCount;
                ++i)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    float3 addDir = addLight.direction;
                    float addNdotL = dot(normalWS, addDir);
                    float distAtten = addLight.distanceAttenuation;
                    float shadowAttenAdd = addLight.shadowAttenuation;
                    float smartShadowAdd = CalculateSmartShadow(shadowAttenAdd, _ShadowReceiveThreshold, _ShadowReceiveSmoothness);
                    float combinedAtten = distAtten * smartShadowAdd;

                    float steppedNdotL = clamp(addNdotL * combinedAtten, -1.0, 1.0);
                    float addRamp = CalculateCelRamp(steppedNdotL, _RampBands, _RampOffset, _RampSmoothness);

                    finalColor += albedo * addLight.color * addRamp;
                }

                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                float rimVal = smoothstep(_RimThreshold, _RimThreshold + _RimSmoothness, fresnel);
                finalColor += _RimColor.rgb * rimVal * _RimIntensity;

                return float4(finalColor, 1.0);
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

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                        output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                output.uv = input.uv;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            float4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(alpha - _Cutoff);
                float3 normalWS = normalize(input.normalWS);
                return float4(normalWS, 0.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }
            Cull Off

            HLSLPROGRAM
            #pragma vertex MetaVertex
            #pragma fragment MetaFragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            struct MetaAttributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct MetaVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            MetaVaryings MetaVertex(MetaAttributes input)
            {
                MetaVaryings output = (MetaVaryings)0;
                output.positionCS = MetaVertexPosition(input.positionOS, input.uv, input.uv, input.lightmapUV, input.lightmapUV);
                output.uv = input.uv;
                return output;
            }

            half4 MetaFragment(MetaVaryings input) : SV_Target
            {
                float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return mainTex;
            }
            ENDHLSL
        }
    }
}
