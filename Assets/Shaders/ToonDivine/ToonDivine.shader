Shader "Stylized/Divine Toon URP 2025"
{
    Properties
    {
        [Header(Core)]
        _MainTex ("Albedo (RGB) FaceShadow (A)", 2D) = "white"{}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0.3, 0.3, 0.5, 1)
        _AmbientColor ("Ambient Color", Color) = (0.0, 0.0, 0.0, 1)

        [Header(Dynamic Ramp)]
        [IntRange] _RampBands ("Ramp Bands", Range(1, 8)) = 2
        _RampOffset ("Ramp Offset", Range(-1, 1)) = 0.0
        _RampSmoothness ("Band Smoothness", Range(0.001, 0.5)) = 0.01

        [Header(Shadow Reception)]
        _ShadowReceiveThreshold ("Shadow Threshold", Range(0, 1)) = 0.5
        _ShadowReceiveSmoothness ("Shadow Smoothness", Range(0.001, 1)) = 0.05

        [Header(Specular)]
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularSize ("Specular Size", Range(0.0, 1.0)) = 0.05
        _SpecularSmoothness ("Specular Smoothness", Range(0.001, 0.5)) = 0.02

        [Header(Rim Light)]
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimThreshold ("Rim Threshold", Range(0, 1)) = 0.5
        _RimSmoothness ("Rim Smoothness", Range(0.001, 1)) = 0.1
        _RimIntensity ("Rim Intensity", Range(0, 10)) = 1

        [Header(System)]
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _BaseColor;
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

            float CalculateCelRamp(float value, float bands, float offset, float smoothness)
            {
                float val = value * 0.5 + 0.5;
                val = saturate(val + offset);

                float x = val * bands;
                float level = floor(x);
                float fractVal = frac(x);
                float smoothPart = smoothstep(0.5 - smoothness, 0.5 + smoothness, fractVal);

                return (level + smoothPart) / bands;
            }

                // Custom Smart Shadow Logic
            float CalculateReceivedShadow(float shadowAtten, float threshold, float smoothness)
            {
                    // Remap shadow attenuation to Stylized Curve
                return smoothstep(threshold - smoothness, threshold + smoothness, shadowAtten);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float faceShadow = mainTex.a;
                float3 albedo = mainTex.rgb * _BaseColor.rgb;

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                    // --- Main Light ---
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lightDir = mainLight.direction;

                    // Smart Shadow Calculation
                float shadowAttenRaw = mainLight.shadowAttenuation;
                float shadowMask = CalculateReceivedShadow(shadowAttenRaw, _ShadowReceiveThreshold, _ShadowReceiveSmoothness);

                float ndotl = dot(normalWS, lightDir);
                float faceShadowFactor = lerp(-1.0, 1.0, faceShadow);
                float litNdotL = min(ndotl, faceShadowFactor);

                float ramp = CalculateCelRamp(litNdotL, _RampBands, _RampOffset, _RampSmoothness);

                    // Apply Smart Shadow Mask to Ramp
                ramp *= shadowMask;

                float3 diffuseColor = lerp(_ShadowColor.rgb, _BaseColor.rgb, ramp);
                diffuseColor *= mainLight.color;

                    // --- Ambient ---
                diffuseColor += _AmbientColor.rgb;

                    // --- Specular ---
                float3 halfDir = normalize(lightDir + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));

                float specThreshold = 1.0 - _SpecularSize;
                float spec = smoothstep(specThreshold, specThreshold + _SpecularSmoothness, NdotH);

                float litMask = saturate(ndotl * 20.0);
                float faceMask = step(0.5, faceShadow);
                spec *= shadowMask * litMask * faceMask;

                float3 specularColor = spec * _SpecularColor.rgb * mainLight.color;

                    // --- Combine Main ---
                float3 finalColor = albedo * diffuseColor + specularColor;

                    // --- Additional Lights ---
                int pixelLightCount = GetAdditionalLightsCount();
                for (int i = 0;
                i < pixelLightCount;
                ++i)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    float3 addDir = addLight.direction;
                    float addNdotL = dot(normalWS, addDir);

                        // Combine attenuation first
                    float distAtten = addLight.distanceAttenuation;
                    float shadowAttenAdd = addLight.shadowAttenuation;

                        // Apply Smart Shadow to Additional Lights as well
                    float smartShadowAdd = CalculateReceivedShadow(shadowAttenAdd, _ShadowReceiveThreshold, _ShadowReceiveSmoothness);

                    float combinedAtten = distAtten * smartShadowAdd;

                        // Minions Art Logic: Multiply NdotL by attenuation
                    float steppedNdotL = addNdotL * combinedAtten;
                    steppedNdotL = clamp(steppedNdotL, -1.0, 1.0);

                    float addRamp = CalculateCelRamp(steppedNdotL, _RampBands, _RampOffset, _RampSmoothness);
                    finalColor += albedo * addLight.color * addRamp;
                }

                    // --- Rim Light ---
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
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
                return output;
            }

            float4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
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
                float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor;
                return albedo;
            }
            ENDHLSL
        }
    }
}
