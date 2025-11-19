Shader "Custom/Toon Lit Triplanar Terrain URP" {
    Properties {
        [Header(Main Properties)]
        _Color ("Main Color", Color) = (1,1,1,1)
        _Tint ("Tint Color", Color) = (1,1,1,1)
        _AmbientColor ("Ambient Color", Color) = (0.1, 0.1, 0.1, 1)

        [Header(Triplanar Textures)]
        _MainTex ("Top Texture", 2D) = "white" {}
        _NormalT ("Top Normal", 2D) = "bump" {}
        _MainTexSide ("Side/Bottom Texture", 2D) = "white" {}
        _Normal ("Side/Bottom Normal", 2D) = "bump" {}
        
        [Header(Triplanar Settings)]
        _Scale ("Top Scale", Range(0.01, 5)) = 1
        _SideScale ("Side Scale", Range(0.01, 5)) = 1
        _Noise ("Noise", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Range(0.01, 5)) = 1

        [Header(Blending)]
        _TopSpread ("Top Blend Start", Range(-1, 2)) = 1
        _EdgeWidth ("Blend Smoothness", Range(0.001, 1)) = 0.1

        [Header(Toon Lighting)]
        _Ramp ("Toon Ramp (RGB)", 2D) = "gray" {}
        _RimPower ("Rim Power", Range(0.1, 20)) = 3
        _RimColor ("Rim Color Top", Color) = (0.8, 0.8, 1, 1)
        _RimColor2 ("Rim Color Side/Bottom", Color) = (0.8, 1, 0.8, 1)
    }

    SubShader {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Color, _Tint, _AmbientColor;
            half _Scale, _SideScale, _NoiseScale;
            half _TopSpread, _EdgeWidth;
            half _RimPower;
            half4 _RimColor, _RimColor2;
        CBUFFER_END

        Texture2D _MainTex;     SamplerState sampler_MainTex;
        Texture2D _NormalT;     SamplerState sampler_NormalT;
        Texture2D _MainTexSide; SamplerState sampler_MainTexSide;
        Texture2D _Normal;      SamplerState sampler_Normal;
        Texture2D _Ramp;        SamplerState sampler_Ramp;
        Texture2D _Noise;       SamplerState sampler_Noise;
        
        struct Attributes {
            float4 positionOS   : POSITION;
            float3 normalOS     : NORMAL;
            float2 uv           : TEXCOORD0;
            float2 lightmapUV   : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings {
            float4 positionCS     : SV_POSITION;
            float3 worldPos       : TEXCOORD0;
            float3 worldNormal    : TEXCOORD1;
            float3 viewDir        : TEXCOORD2;
            DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                half4 fogFactorAndVertexLight : TEXCOORD4;
            #else
                half fogFactor : TEXCOORD4;
            #endif
            float4 shadowCoord    : TEXCOORD5;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        half3 UnpackNormalFromTexture(half4 packedNormal) {
            #if defined(UNITY_NO_DXT5nm)
                return packedNormal.xyz * 2.0 - 1.0;
            #else
                half3 normal;
                normal.xy = packedNormal.wy * 2.0 - 1.0;
                normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
                return normal;
            #endif
        }

        void CalculateTriplanarNormal(float3 worldPos, float3 worldNormal, float3 blendWeights, out half3 triplanarNormal,
                                      Texture2D normalMap, SamplerState ss, half texScale)
        {
            half3 normalTexVal = lerp(
                lerp(UnpackNormalFromTexture(SAMPLE_TEXTURE2D(normalMap, ss, worldPos.xy * texScale)),
                     UnpackNormalFromTexture(SAMPLE_TEXTURE2D(normalMap, ss, worldPos.zy * texScale)),
                     blendWeights.x),
                     UnpackNormalFromTexture(SAMPLE_TEXTURE2D(normalMap, ss, worldPos.zx * texScale)),
                     blendWeights.y);

            half3 axisSign = sign(worldNormal);
            half3 tnormalX = half3(normalTexVal.xy + worldNormal.zy, abs(normalTexVal.z) * worldNormal.x); tnormalX.z *= axisSign.x;
            half3 tnormalY = half3(normalTexVal.xy + worldNormal.xz, abs(normalTexVal.z) * worldNormal.y); tnormalY.z *= axisSign.y;
            half3 tnormalZ = half3(normalTexVal.xy + worldNormal.xy, abs(normalTexVal.z) * worldNormal.z); tnormalZ.z *= axisSign.z;
            triplanarNormal = normalize(tnormalX.zyx * blendWeights.x + tnormalY.xzy * blendWeights.y + tnormalZ.xyz * blendWeights.z);
        }

        Varyings MainVert(Attributes input) {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            
            VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
            VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
            
            output.positionCS = vertexInput.positionCS;
            output.worldPos = vertexInput.positionWS;
            output.worldNormal = normalInput.normalWS;
            output.viewDir = GetWorldSpaceNormalizeViewDir(output.worldPos);
            
            OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
            OUTPUT_SH(output.worldNormal, output.vertexSH);
            
            output.fogFactor = ComputeFogFactor(output.positionCS.z);
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                output.fogFactorAndVertexLight = half4(output.fogFactor, VertexLighting(output.worldPos, output.worldNormal));
            #endif
            
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(vertexInput);
            #endif
            return output;
        }

        void GetSurfaceData(Varyings input, out half3 albedo, out half3 normalWS, out half3 emission) {
            half3 worldNormal = normalize(input.worldNormal);
            half3 viewDir = normalize(input.viewDir);
            half3 blendWeights = saturate(pow(abs(worldNormal), 4));

            half3 noiseTexture = lerp(lerp(SAMPLE_TEXTURE2D(_Noise, sampler_Noise, input.worldPos.xy * _NoiseScale).rgb, SAMPLE_TEXTURE2D(_Noise, sampler_Noise, input.worldPos.zy * _NoiseScale).rgb, blendWeights.x), SAMPLE_TEXTURE2D(_Noise, sampler_Noise, input.worldPos.xz * _NoiseScale).rgb, blendWeights.y);
            half noiseOffset = noiseTexture.g + (noiseTexture.r + noiseTexture.b) * 0.5 - 0.5;
            half worldNormalDotNoise = worldNormal.y + noiseOffset;

            half3 topTex = lerp(lerp(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.worldPos.xy * _Scale).rgb, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.worldPos.zy * _Scale).rgb, blendWeights.x), SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.worldPos.zx * _Scale).rgb, blendWeights.y);
            half3 sideTex = lerp(lerp(SAMPLE_TEXTURE2D(_MainTexSide, sampler_MainTexSide, input.worldPos.xy * _SideScale).rgb, SAMPLE_TEXTURE2D(_MainTexSide, sampler_MainTexSide, input.worldPos.zy * _SideScale).rgb, blendWeights.x), SAMPLE_TEXTURE2D(_MainTexSide, sampler_MainTexSide, input.worldPos.zx * _SideScale).rgb, blendWeights.y);
            
            half3 topNormalResult, sideNormalResult;
            CalculateTriplanarNormal(input.worldPos, worldNormal, blendWeights, topNormalResult, _NormalT, sampler_NormalT, _Scale);
            CalculateTriplanarNormal(input.worldPos, worldNormal, blendWeights, sideNormalResult, _Normal, sampler_Normal, _SideScale);
            
            half blendFactor = smoothstep(_TopSpread, _TopSpread + _EdgeWidth, worldNormalDotNoise);

            normalWS = normalize(lerp(sideNormalResult, topNormalResult, blendFactor));
            albedo = lerp(sideTex, topTex, blendFactor) * _Color.rgb;

            half rim = 1.0 - saturate(dot(viewDir, normalWS));
            half3 rimColor = lerp(_RimColor2.rgb, _RimColor.rgb, blendFactor);
            emission = pow(rim, _RimPower) * rimColor;
        }

        ENDHLSL

        Pass {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex MainVert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fog

            half4 frag(Varyings input) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 albedo, normalWS, emission;
                GetSurfaceData(input, albedo, normalWS, emission);

                InputData inputData;
                inputData.positionWS = input.worldPos;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = normalize(input.viewDir);
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                #endif
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                Light mainLight = GetMainLight(input.shadowCoord);
                half atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                half d = saturate(dot(normalWS, mainLight.direction) * 0.5 + 0.5);
                half3 ramp = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, half2(d,d)).rgb;
                half3 directLighting = albedo * mainLight.color * ramp * atten;

                #ifdef _ADDITIONAL_LIGHTS
                    uint lightCount = GetAdditionalLightsCount();
                    for (uint i = 0; i < lightCount; ++i) {
                        Light light = GetAdditionalLight(i, input.worldPos);
                        atten = light.shadowAttenuation * light.distanceAttenuation;
                        d = saturate(dot(normalWS, light.direction) * 0.5 + 0.5);
                        ramp = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, half2(d,d)).rgb;
                        directLighting += albedo * light.color * ramp * atten;
                    }
                #endif
                
                half3 indirectLighting = inputData.bakedGI * _AmbientColor.rgb;
                half3 finalColor = (directLighting + indirectLighting * albedo) * _Tint.rgb;
                finalColor += emission * _Tint.rgb;

                return half4(MixFog(finalColor, inputData.fogCoord), 1.0);
            }
            ENDHLSL
        }

        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            struct ShadowVaryings {
                float4 positionCS : SV_POSITION;
            };

            ShadowVaryings vert(Attributes input) {
                ShadowVaryings o;
                UNITY_SETUP_INSTANCE_ID(input);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                Light mainLight = GetMainLight();
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(vertexInput.positionWS, normalWS, mainLight.direction));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = positionCS;
                return o;
            }
            half4 frag(ShadowVaryings i) : SV_Target { return 0; }
            ENDHLSL
        }
        
        Pass {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ZTest LEqual Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct DepthVaryings { float4 positionCS : SV_POSITION; };
            DepthVaryings vert(Attributes input) {
                DepthVaryings o;
                UNITY_SETUP_INSTANCE_ID(input);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }
            half4 frag(DepthVaryings i) : SV_TARGET { return 0; }
            ENDHLSL
        }

        Pass {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On ZTest LEqual Cull Back

            HLSLPROGRAM
            #pragma vertex MainVert
            #pragma fragment frag

            half4 frag(Varyings input) : SV_TARGET {
                half3 albedo, normalWS, emission;
                GetSurfaceData(input, albedo, normalWS, emission);
                return half4(EncodeViewNormal(normalWS), 0);
            }
            ENDHLSL
        }

        Pass {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            struct MetaVaryings {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            MetaVaryings vert(Attributes input) {
                MetaVaryings o;
                o.positionCS = MetaVertexPosition(input.positionOS, input.uv, input.lightmapUV);
                o.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                return o;
            }

            half4 frag(MetaVaryings input) : SV_Target {
                half3 worldNormal = float3(0,1,0); 
                half3 blendWeights = saturate(pow(abs(worldNormal), 4));

                half3 noiseTexture = lerp(lerp(SAMPLE_TEXTURE2D(_Noise, sampler_Noise, input.worldPos.xy * _NoiseScale).rgb, SAMPLE_TEXTURE2D(_Noise, sampler_Noise, input.worldPos.zy * _NoiseScale).rgb, blendWeights.x), SAMPLE_TEXTURE2D(_Noise, sampler_Noise, input.worldPos.xz * _NoiseScale).rgb, blendWeights.y);
                half noiseOffset = noiseTexture.g + (noiseTexture.r + noiseTexture.b) * 0.5 - 0.5;
                half worldNormalDotNoise = worldNormal.y + noiseOffset;

                half3 topTex = lerp(lerp(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.worldPos.xy * _Scale).rgb, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.worldPos.zy * _Scale).rgb, blendWeights.x), SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.worldPos.zx * _Scale).rgb, blendWeights.y);
                half3 sideTex = lerp(lerp(SAMPLE_TEXTURE2D(_MainTexSide, sampler_MainTexSide, input.worldPos.xy * _SideScale).rgb, SAMPLE_TEXTURE2D(_MainTexSide, sampler_MainTexSide, input.worldPos.zy * _SideScale).rgb, blendWeights.x), SAMPLE_TEXTURE2D(_MainTexSide, sampler_MainTexSide, input.worldPos.zx * _SideScale).rgb, blendWeights.y);
                
                half blendFactor = smoothstep(_TopSpread, _TopSpread + _EdgeWidth, worldNormalDotNoise);
                half3 albedo = lerp(sideTex, topTex, blendFactor) * _Color.rgb;
                
                UnityMetaInput metaInput;
                metaInput.Albedo = albedo;
                metaInput.Emission = half3(0,0,0);
                return UnityMetaFragment(metaInput);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
    CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
}