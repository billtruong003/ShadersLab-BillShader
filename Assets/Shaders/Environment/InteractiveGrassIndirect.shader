Shader "CleanCode/InteractiveGrass_Indirect"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white"{}
        [NoScaleOffset] _AlphaMask("Alpha Clip Mask (R)", 2D) = "white"{}
        [HDR] [MainColor] _TopColor("Top Color", Color) = (0.4, 0.8, 0.4, 1)
        [HDR] _BottomColor("Bottom Color", Color) = (0.1, 0.3, 0.1, 1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _GroundBlend("Ground Blend Height", Range(0.001, 1.0)) = 0.2
        _SSAONormalFlatten("SSAO Normal Flatten", Range(0.0, 1.0)) = 0.5

        [Header(Translucency)]
        [HDR] _TranslucencyColor("Translucency Color", Color) = (0.8, 1.0, 0.2, 1)
        _TranslucencyGain("Translucency Gain", Range(0.0, 5.0)) = 1.0
        _TranslucencyDistortion("Translucency Distortion", Range(0.0, 1.0)) = 0.5
        _TranslucencyPower("Translucency Power", Range(1.0, 10.0)) = 4.0

        [Header(Emission)]
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 0)

        [Header(Wind)]
        _WindSpeed("Wind Speed", Float) = 1.5
        _WindStrength("Wind Strength", Float) = 0.2

        [Header(Interaction)]
        _InteractionRadius("Interaction Radius", Float) = 2.0
        _InteractionStrength("Interaction Strength", Float) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        Cull Off
        ZWrite On
        ZTest LEqual

        // 1. Depth Only Pass - Crucial for Occlusion & Post Processing
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ColorMask 0
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:SetupIndirect
            
            #include "IndirectIncludes.hlsl"
            #include "FoliageInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS = ApplyWindAndInteraction(positionWS, input.uv);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                half mask = SAMPLE_TEXTURE2D(_AlphaMask, sampler_BaseMap, input.uv).r;
                half blend = smoothstep(0.0, _GroundBlend, input.uv.y);
                clip((alpha * mask * blend) - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // 2. Depth Normals Pass - For SSAO and Outline Normals
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:SetupIndirect
            
            #include "IndirectIncludes.hlsl"
            #include "FoliageInput.hlsl"

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
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS = ApplyWindAndInteraction(positionWS, input.uv);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                half mask = SAMPLE_TEXTURE2D(_AlphaMask, sampler_BaseMap, input.uv).r;
                half blend = smoothstep(0.0, _GroundBlend, input.uv.y);
                clip((alpha * mask * blend) - _Cutoff);

                float3 normal = normalize(input.normalWS);
                normal = normalize(lerp(normal, float3(0, 1, 0), _SSAONormalFlatten));
                return float4(NormalizeNormalPerPixel(normal), 0.0);
            }
            ENDHLSL
        }

        // 3. Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ColorMask 0
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:SetupIndirect
            
            #include "IndirectIncludes.hlsl"
            #include "FoliageInput.hlsl"

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
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS = ApplyWindAndInteraction(positionWS, input.uv);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                half mask = SAMPLE_TEXTURE2D(_AlphaMask, sampler_BaseMap, input.uv).r;
                half blend = smoothstep(0.0, _GroundBlend, input.uv.y);
                clip((alpha * mask * blend) - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // 4. Forward Lit Pass
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:SetupIndirect
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "IndirectIncludes.hlsl"
            #include "FoliageInput.hlsl"

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
                float3 positionWS : TEXCOORD1;
                float3 normalWS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS = ApplyWindAndInteraction(positionWS, input.uv);

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half mask = SAMPLE_TEXTURE2D(_AlphaMask, sampler_BaseMap, input.uv).r;
                half blend = smoothstep(0.0, _GroundBlend, input.uv.y);
                clip((baseMap.a * mask * blend) - _Cutoff);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 N = normalize(input.normalWS);
                half3 L = normalize(mainLight.direction);
                half3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);

                half NdotL = saturate(dot(N, L));
                half3 translucency = CalculateTranslucency(L, N, V);
                half3 ambient = SampleSH(N);
                half3 baseColor = lerp(_BottomColor.rgb, _TopColor.rgb, input.uv.y);

                half3 diffuse = baseColor * (ambient + (mainLight.color * (NdotL + translucency) * mainLight.shadowAttenuation));

                return half4(diffuse + _EmissionColor.rgb, 1.0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "SelectionMask"
            Tags { "LightMode" = "SelectionMask" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma instancing_options procedural : SetupIndirect
            #include "IndirectIncludes.hlsl"
            #include "FoliageInput.hlsl"

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
            };
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS = ApplyWindAndInteraction(positionWS, input.uv);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                clip((SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * smoothstep(0, _GroundBlend, input.uv.y)) - _Cutoff);
                return half4(1, 0, 0, 1);
            }
            ENDHLSL
        }
    }
}