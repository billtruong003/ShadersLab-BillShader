Shader "BillWater/URPWaterBlingSparkle"
{
    Properties
    {
        [Header(Colors and Depth)]
        _ShallowColor("Shallow Color", Color) = (0.3, 0.8, 0.9, 0.7)
        [HDR] _DeepColor("Deep Color", Color) = (0.0, 0.2, 0.4, 0.8)
        _DepthMaxDistance("Depth Max Distance", Range(0, 10)) = 3.0

        [Header(Surface Normals and Refraction)]
        _NormalMapA("Normal A", 2D) = "bump"{}
        _NormalTilingA("Normal Tiling A", Float) = 0.8
        _NormalScrollA("Normal Scroll A", Vector) = (0.01, 0.01, 0, 0)
        _NormalMapB("Normal B", 2D) = "bump"{}
        _NormalTilingB("Normal Tiling B", Float) = 1.2
        _NormalScrollB("Normal Scroll B", Vector) = (-0.012, 0.008, 0, 0)
        _RefractionStrength("Refraction Strength", Range(0.0, 0.1)) = 0.025

        [Header(Surface Foam)]
        _SurfaceFoamTexture("Surface Foam Texture", 2D) = "white"{}
        _SurfaceFoamTiling("Surface Foam Tiling", Float) = 1.0
        _SurfaceFoamScroll("Surface Foam Scroll", Vector) = (0.02, 0.025, 0, 0)
        _SurfaceFoamCutoff("Surface Foam Cutoff", Range(0, 1)) = 0.5
        _SurfaceFoamDistortionMap("Foam Distortion Map", 2D) = "gray"{}
        _SurfaceFoamDistortionStrength("Foam Distortion Strength", Range(0, 0.2)) = 0.05

        [Header(Bling Sparkle)]
        _BlingNoiseMap("Bling Noise Map", 2D) = "white"{}
        [HDR] _BlingColor("Bling Color", Color) = (1.5, 1.5, 1.5, 1.0)
        _BlingIntensity("Bling Intensity", Range(0, 10)) = 2.0
        _BlingScale("Bling Scale", Float) = 2.0
        _BlingSpeed("Bling Speed", Range(0, 20)) = 10.0
        _BlingFresnelPower("Bling Fresnel Power", Range(1, 20)) = 8.0
        _BlingThreshold("Bling Threshold", Range(0.5, 1.0)) = 0.98

        [Header(Intersection Foam)]
        _FoamColor("Intersection Foam Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _FoamIntersectionDepth("Intersection Depth", Range(0.01, 5.0)) = 0.5
        _FoamIntersectionSoftness("Intersection Softness", Range(0.01, 5.0)) = 1.0

        [Header(Vertex Waves)]
        _WaveAmplitude("Wave Amplitude", Range(0.0, 1.0)) = 0.1
        _WaveFrequency("Wave Frequency", Float) = 1.0
        _WaveSpeed("Wave Speed", Float) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _ShallowColor, _DeepColor, _FoamColor, _BlingColor;
            float4 _NormalScrollA, _NormalScrollB, _SurfaceFoamScroll;
            float _DepthMaxDistance, _NormalTilingA, _NormalTilingB, _RefractionStrength;
            float _SurfaceFoamTiling, _SurfaceFoamCutoff, _SurfaceFoamDistortionStrength;
            float _BlingScale, _BlingSpeed, _BlingFresnelPower, _BlingThreshold, _BlingIntensity;
            float _FoamIntersectionDepth, _FoamIntersectionSoftness;
            float _WaveAmplitude, _WaveFrequency, _WaveSpeed;
            CBUFFER_END

            TEXTURE2D(_NormalMapA);
            SAMPLER(sampler_NormalMapA);
            TEXTURE2D(_NormalMapB);
            SAMPLER(sampler_NormalMapB);
            TEXTURE2D(_SurfaceFoamTexture);
            SAMPLER(sampler_SurfaceFoamTexture);
            TEXTURE2D(_SurfaceFoamDistortionMap);
            SAMPLER(sampler_SurfaceFoamDistortionMap);
            TEXTURE2D(_BlingNoiseMap);
            SAMPLER(sampler_BlingNoiseMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float time = _Time.y * _WaveSpeed;
                float wave = sin(time + (input.positionOS.x + input.positionOS.z) * _WaveFrequency) * _WaveAmplitude;
                float3 posOS = input.positionOS.xyz;
                posOS.y += wave;

                output.positionWS = TransformObjectToWorld(posOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceDepth = input.screenPos.w;
                float depthDiff = max(0.0, sceneDepth - surfaceDepth);

                float2 uvA = input.positionWS.xz * _NormalTilingA + _Time.y * _NormalScrollA.xy;
                float2 uvB = input.positionWS.xz * _NormalTilingB + _Time.y * _NormalScrollB.xy;
                half3 nA = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMapA, sampler_NormalMapA, uvA));
                half3 nB = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMapB, sampler_NormalMapB, uvB));
                half3 normalWS = normalize(half3(nA.xy + nB.xy, 2.0));

                float2 refractUV = screenUV + normalWS.xy * _RefractionStrength * saturate(depthDiff);
                half3 sceneColor = SampleSceneColor(refractUV);

                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, saturate(depthDiff / _DepthMaxDistance));
                half3 finalColor = lerp(waterColor, sceneColor, 1.0 - _ShallowColor.a);

                float2 distUV = input.positionWS.xz * 0.25 + _Time.y * 0.01;
                float2 distOffset = (SAMPLE_TEXTURE2D(_SurfaceFoamDistortionMap, sampler_SurfaceFoamDistortionMap, distUV).xy * 2.0 - 1.0) * _SurfaceFoamDistortionStrength;

                float2 foamUV = input.positionWS.xz * _SurfaceFoamTiling + _Time.y * _SurfaceFoamScroll.xy + distOffset;
                float foamNoise = SAMPLE_TEXTURE2D(_SurfaceFoamTexture, sampler_SurfaceFoamTexture, foamUV).r;
                float surfFoam = step(_SurfaceFoamCutoff, foamNoise);

                float intersectFoam = 1.0 - saturate(depthDiff / _FoamIntersectionDepth);
                intersectFoam = smoothstep(0.0, _FoamIntersectionSoftness, intersectFoam);

                half foamAmount = saturate(intersectFoam + surfFoam);
                finalColor = lerp(finalColor, _FoamColor.rgb, foamAmount);

                half3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                half NdotV = 1.0 - saturate(dot(normalWS, viewDir));
                half fresnel = pow(NdotV, _BlingFresnelPower);

                float2 blingUV = input.screenPos.xy / input.screenPos.w * _BlingScale;
                blingUV.x *= _ScreenParams.x / _ScreenParams.y;
                float2 blingScroll = _Time.y * _BlingSpeed * 0.05;

                half noise1 = SAMPLE_TEXTURE2D(_BlingNoiseMap, sampler_BlingNoiseMap, blingUV + blingScroll).r;
                half noise2 = SAMPLE_TEXTURE2D(_BlingNoiseMap, sampler_BlingNoiseMap, blingUV * 1.5 - blingScroll).r;
                half sparkle = smoothstep(_BlingThreshold, 1.0, noise1 * noise2);

                finalColor += sparkle * fresnel * _BlingColor.rgb * _BlingIntensity;

                return half4(finalColor, _ShallowColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
            float _WaveAmplitude, _WaveFrequency, _WaveSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float time = _Time.y * _WaveSpeed;
                float wave = sin(time + (input.positionOS.x + input.positionOS.z) * _WaveFrequency) * _WaveAmplitude;
                float3 posOS = input.positionOS.xyz;
                posOS.y += wave;

                output.positionCS = TransformObjectToHClip(posOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
            float _WaveAmplitude, _WaveFrequency, _WaveSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float time = _Time.y * _WaveSpeed;
                float wave = sin(time + (input.positionOS.x + input.positionOS.z) * _WaveFrequency) * _WaveAmplitude;
                float3 posOS = input.positionOS.xyz;
                posOS.y += wave;

                float3 positionWS = TransformObjectToWorld(posOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                        output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
}
