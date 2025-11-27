
Shader "Custom/OptimizedToonTriplanar"
{
    Properties
    {
        [Header(Main Properties)]
        [MainColor] _Color("Main Color", Color) = (1, 1, 1, 1)
        _Tint("Tint Color", Color) = (1, 1, 1, 1)
        _AmbientColor("Ambient Color", Color) = (0.1, 0.1, 0.1, 1)
        [Header(Triplanar Textures)]
        [NoScaleOffset] _MainTex("Top Texture", 2D) = "white"{}
        [NoScaleOffset] _NormalT("Top Normal", 2D) = "bump"{}
        [NoScaleOffset] _MainTexSide("Side Bottom Texture", 2D) = "white"{}
        [NoScaleOffset] _Normal("Side Bottom Normal", 2D) = "bump"{}

        [Header(Triplanar Settings)]
        _Scale("Top Scale", Float) = 1
        _SideScale("Side Scale", Float) = 1
        [NoScaleOffset] _Noise("Noise", 2D) = "white"{}
        _NoiseScale("Noise Scale", Float) = 1

        [Header(Blending)]
        _TopSpread("Top Blend Start", Range(-1, 2)) = 1
        _EdgeWidth("Blend Smoothness", Range(0.001, 1)) = 0.1

        [Header(Toon Lighting)]
        [NoScaleOffset] _Ramp("Toon Ramp (RGB)", 2D) = "gray"{}
        _RimPower("Rim Power", Range(0.1, 20)) = 3
        _RimColor("Rim Color Top", Color) = (0.8, 0.8, 1, 1)
        _RimColor2("Rim Color Side Bottom", Color) = (0.8, 1, 0.8, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
        half4 _Color;
        half4 _Tint;
        half4 _AmbientColor;
        float _Scale;
        float _SideScale;
        float _NoiseScale;
        float _TopSpread;
        float _EdgeWidth;
        float _RimPower;
        half4 _RimColor;
        half4 _RimColor2;
        float4 _MainTex_ST;
        CBUFFER_END

        Texture2D _MainTex; SamplerState sampler_MainTex;
        Texture2D _NormalT; SamplerState sampler_NormalT;
        Texture2D _MainTexSide; SamplerState sampler_MainTexSide;
        Texture2D _Normal; SamplerState sampler_Normal;
        Texture2D _Ramp; SamplerState sampler_Ramp;
        Texture2D _Noise; SamplerState sampler_Noise;

        half2 CustomEncodeViewNormal(half3 n)
        {
            return n.xy * 0.5 + 0.5;
        }

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 lightmapUV : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 2);
            float4 shadowCoord : TEXCOORD3;
            float fogFactor : TEXCOORD4;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        half3 BlendTriplanarNormal(float3 positionWS, float3 normalWS, float3 blendWeights, Texture2D normalMap, SamplerState ss, float scale)
        {
            float2 uvX = positionWS.zy * scale;
            float2 uvY = positionWS.xz * scale;
            float2 uvZ = positionWS.xy * scale;

            half3 tnormalX = UnpackNormal(SAMPLE_TEXTURE2D(normalMap, ss, uvX));
            half3 tnormalY = UnpackNormal(SAMPLE_TEXTURE2D(normalMap, ss, uvY));
            half3 tnormalZ = UnpackNormal(SAMPLE_TEXTURE2D(normalMap, ss, uvZ));

            tnormalX.z += 0.00001;
            tnormalY.z += 0.00001;
            tnormalZ.z += 0.00001;

            return normalize(tnormalX.zyx * blendWeights.x + tnormalY.xzy * blendWeights.y + tnormalZ.xyz * blendWeights.z);
        }

        void GetSurfaceData(float3 positionWS, float3 normalWS, out half3 albedo, out half3 finalNormal, out half blendFactor)
        {
            float3 blendWeights = pow(abs(normalWS), 4.0);
            blendWeights /= dot(blendWeights, 1.0);

            float2 uvX = positionWS.zy;
            float2 uvY = positionWS.xz;
            float2 uvZ = positionWS.xy;

            half3 noiseX = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, uvX * _NoiseScale).rgb;
            half3 noiseY = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, uvY * _NoiseScale).rgb;
            half3 noiseZ = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, uvZ * _NoiseScale).rgb;

            half3 noiseTex = noiseX * blendWeights.x + noiseY * blendWeights.y + noiseZ * blendWeights.z;
            half noiseVal = noiseTex.g + (noiseTex.r + noiseTex.b) * 0.5 - 0.5;

            half worldNormalDotNoise = normalWS.y + noiseVal;
            blendFactor = smoothstep(_TopSpread, _TopSpread + _EdgeWidth, worldNormalDotNoise);

            half3 colTopX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvX * _Scale).rgb;
            half3 colTopY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvY * _Scale).rgb;
            half3 colTopZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvZ * _Scale).rgb;
            half3 colTop = colTopX * blendWeights.x + colTopY * blendWeights.y + colTopZ * blendWeights.z;

            half3 colSideX = SAMPLE_TEXTURE2D(_MainTexSide, sampler_MainTexSide, uvX * _SideScale).rgb;
            half3 colSideY = SAMPLE_TEXTURE2D(_MainTexSide, sampler_MainTexSide, uvY * _SideScale).rgb;
            half3 colSideZ = SAMPLE_TEXTURE2D(_MainTexSide, sampler_MainTexSide, uvZ * _SideScale).rgb;
            half3 colSide = colSideX * blendWeights.x + colSideY * blendWeights.y + colSideZ * blendWeights.z;

            albedo = lerp(colSide, colTop, blendFactor) * _Color.rgb;

            half3 normTop = BlendTriplanarNormal(positionWS, normalWS, blendWeights, _NormalT, sampler_NormalT, _Scale);
            half3 normSide = BlendTriplanarNormal(positionWS, normalWS, blendWeights, _Normal, sampler_Normal, _SideScale);

            finalNormal = normalize(lerp(normSide, normTop, blendFactor));
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
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

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                output.normalWS = normalInput.normalWS;

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);

                output.shadowCoord = GetShadowCoord(vertexInput);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 normalWS = normalize(input.normalWS);

                half3 albedo, pixelNormal;
                half blendFactor;
                GetSurfaceData(input.positionWS, normalWS, albedo, pixelNormal, blendFactor);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = pixelNormal;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, pixelNormal);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
                half3 mainLightColor = mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                half NdotL = dot(pixelNormal, mainLight.direction) * 0.5 + 0.5;
                half3 ramp = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, half2(NdotL, 0.5)).rgb;
                half3 directLighting = albedo * mainLightColor * ramp;

                #ifdef _ADDITIONAL_LIGHTS
                    uint pixelLightCount = GetAdditionalLightsCount();
                    for (uint i = 0; i < pixelLightCount; ++i)
                    {
                        Light light = GetAdditionalLight(i, inputData.positionWS, inputData.shadowMask);
                        half addNdotL = dot(pixelNormal, light.direction) * 0.5 + 0.5;
                        half3 addRamp = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, half2(addNdotL, 0.5)).rgb;
                        directLighting += albedo * light.color * light.distanceAttenuation * light.shadowAttenuation * addRamp;
                    }
                #endif

                half3 indirectLighting = inputData.bakedGI * _AmbientColor.rgb * albedo;

                half rimDot = 1.0 - saturate(dot(viewDirWS, pixelNormal));
                half3 rimColor = lerp(_RimColor2.rgb, _RimColor.rgb, blendFactor);
                half3 emission = pow(rimDot, _RimPower) * rimColor;

                half3 finalColor = (directLighting + indirectLighting) * _Tint.rgb + emission;
                return half4(MixFog(finalColor, inputData.fogCoord), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            ShadowVaryings ShadowPassVertex(ShadowAttributes input)
            {
                ShadowVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            half4 ShadowPassFragment(ShadowVaryings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthVaryings DepthOnlyVertex(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(DepthVaryings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                output.normalWS = normalInput.normalWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                half3 albedo, pixelNormal;
                half blendFactor;
                GetSurfaceData(input.positionWS, normalWS, albedo, pixelNormal, blendFactor);
                float3 normalVS = TransformWorldToViewNormal(pixelNormal, true);
                return half4(CustomEncodeViewNormal(normalVS), 0.0, 0.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert_meta
            #pragma fragment frag_meta
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            struct MetaAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct MetaVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            MetaVaryings vert_meta(MetaAttributes input)
            {
                MetaVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = MetaVertexPosition(input.positionOS, input.lightmapUV, input.uv, unity_LightmapST, float4(input.normalOS, 0.0));
                return output;
            }

            half4 frag_meta(MetaVaryings input) : SV_Target
            {
                half3 albedo, pixelNormal;
                half blendFactor;
                GetSurfaceData(input.positionWS, normalize(input.normalWS), albedo, pixelNormal, blendFactor);
                UnityMetaInput metaInput;
                metaInput.Albedo = albedo;
                metaInput.Emission = half3(0, 0, 0);
                return UnityMetaFragment(metaInput);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}