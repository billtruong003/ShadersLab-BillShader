Shader "CleanCode/IndirectGrass"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [HDR] _TopColor("Top Color", Color) = (0.5, 0.8, 0.3, 1)
        [HDR] _BottomColor("Bottom Color", Color) = (0.1, 0.3, 0.1, 1)
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        
        [Header(Wind)]
        _WindSpeed("Wind Speed", Float) = 1.0
        _WindStrength("Wind Strength", Float) = 0.2
        _WindFreq("Wind Frequency", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:Setup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 color : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct IndirectData
            {
                float4x4 objectToWorld;
            };

            StructuredBuffer<IndirectData> _VisibleInstanceData;
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _TopColor;
                half4 _BottomColor;
                half _Cutoff;
                float _WindSpeed;
                float _WindStrength;
                float _WindFreq;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            void Setup()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    IndirectData data = _VisibleInstanceData[unity_InstanceID];
                    unity_ObjectToWorld = data.objectToWorld;
                    unity_WorldToObject = inverse(data.objectToWorld);
                #endif
            }

            float3 ApplyWind(float3 positionWS, float3 normalWS, float2 uv)
            {
                float2 windUV = positionWS.xz * _WindFreq + _Time.y * _WindSpeed;
                float windNoise = sin(windUV.x + windUV.y);
                float3 windOffset = float3(windNoise, 0, windNoise) * _WindStrength * uv.y; // Only top moves
                return positionWS + windOffset;
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                positionWS = ApplyWind(positionWS, normalWS, input.uv);

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = lerp(_BottomColor.rgb, _TopColor.rgb, input.uv.y);
                
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(texColor.a - _Cutoff);
                
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 lighting = mainLight.color * (NdotL * mainLight.shadowAttenuation);
                half3 ambient = SampleSH(input.normalWS);
                
                return half4(input.color * texColor.rgb * (lighting + ambient), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:Setup
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct IndirectData { float4x4 objectToWorld; };
            StructuredBuffer<IndirectData> _VisibleInstanceData;
            
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            half _Cutoff;
            float _WindSpeed, _WindStrength, _WindFreq;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            void Setup() {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                unity_ObjectToWorld = _VisibleInstanceData[unity_InstanceID].objectToWorld;
                unity_WorldToObject = inverse(unity_ObjectToWorld);
                #endif
            }

            Varyings Vertex(Attributes input) {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float2 windUV = posWS.xz * _WindFreq + _Time.y * _WindSpeed;
                posWS += float3(sin(windUV.x+windUV.y),0,sin(windUV.x+windUV.y)) * _WindStrength * input.uv.y;

                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(input);
                half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                clip(a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}