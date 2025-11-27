Shader "CleanCode/BakedGrass"
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

            struct GrassInstance
            {
                float3 position;
                float rotY;
                float2 scale;
                uint colorSeed;
                float padding;
            };

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

            StructuredBuffer<GrassInstance> _VisibleBuffer;
            float3 _GlobalInteractorPos;
            float4 _GlobalInteractorParams; 

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
                    GrassInstance data = _VisibleBuffer[unity_InstanceID];
                    unity_ObjectToWorld = 0;
                    unity_ObjectToWorld._m03_m13_m23_m33 = float4(data.position, 1);
                    unity_ObjectToWorld._m00_m11_m22 = float3(1, 1, 1);
                    unity_WorldToObject = unity_ObjectToWorld;
                #endif
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                float3 positionWS = input.positionOS.xyz;
                float3 normalWS = input.normalOS;
                float3 colorVar = _TopColor.rgb;

                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    GrassInstance data = _VisibleBuffer[unity_InstanceID];
                    
                    float3 pos = input.positionOS.xyz;
                    pos.x *= data.scale.x; 
                    pos.y *= data.scale.y;

                    float s, c;
                    sincos(data.rotY * 0.0174532925, s, c);
                    
                    float3 rotatedPos;
                    rotatedPos.x = pos.x * c + pos.z * s;
                    rotatedPos.y = pos.y;
                    rotatedPos.z = pos.z * c - pos.x * s;

                    positionWS = rotatedPos + data.position;
                    
                    float3 rotNormal;
                    rotNormal.x = normalWS.x * c + normalWS.z * s;
                    rotNormal.y = normalWS.y;
                    rotNormal.z = normalWS.z * c - normalWS.x * s;
                    normalWS = normalize(rotNormal);

                    float2 windUV = positionWS.xz * _WindFreq + _Time.y * _WindSpeed;
                    float windNoise = sin(windUV.x + windUV.y + data.colorSeed);
                    positionWS += float3(windNoise, 0, windNoise) * _WindStrength * input.uv.y;

                    float dist = distance(positionWS.xz, _GlobalInteractorPos.xz);
                    float push = 1.0 - saturate(dist / _GlobalInteractorParams.x);
                    push = smoothstep(0, 1, push);
                    
                    float3 pushDir = normalize(positionWS - _GlobalInteractorPos);
                    pushDir.y = -0.5; 
                    positionWS += pushDir * push * _GlobalInteractorParams.y * input.uv.y;

                    float randomHue = frac(data.colorSeed * 0.13);
                    colorVar = lerp(_BottomColor.rgb, _TopColor.rgb, input.uv.y) * (0.8 + 0.4 * randomHue);
                #else
                    positionWS = TransformObjectToWorld(input.positionOS.xyz);
                    normalWS = TransformObjectToWorldNormal(input.normalOS);
                #endif

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = colorVar;

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

            struct GrassInstance { float3 position; float rotY; float2 scale; uint colorSeed; float padding; };
            StructuredBuffer<GrassInstance> _VisibleBuffer;
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST; half _Cutoff; 
            float _WindSpeed, _WindStrength, _WindFreq;
            float3 _GlobalInteractorPos; float4 _GlobalInteractorParams;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            void Setup() {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                GrassInstance data = _VisibleBuffer[unity_InstanceID];
                unity_ObjectToWorld = 0;
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(data.position, 1);
                unity_ObjectToWorld._m00_m11_m22 = float3(1, 1, 1);
                unity_WorldToObject = unity_ObjectToWorld;
                #endif
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                float3 pos = input.positionOS.xyz;

                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    GrassInstance data = _VisibleBuffer[unity_InstanceID];
                    pos.x *= data.scale.x; pos.y *= data.scale.y;
                    float s, c; sincos(data.rotY * 0.0174532925, s, c);
                    float3 rotPos; rotPos.x = pos.x * c + pos.z * s; rotPos.y = pos.y; rotPos.z = pos.z * c - pos.x * s;
                    float3 positionWS = rotPos + data.position;
                    
                    float2 windUV = positionWS.xz * _WindFreq + _Time.y * _WindSpeed;
                    float windNoise = sin(windUV.x + windUV.y + data.colorSeed);
                    positionWS += float3(windNoise, 0, windNoise) * _WindStrength * input.uv.y;

                    float dist = distance(positionWS.xz, _GlobalInteractorPos.xz);
                    float push = smoothstep(0, 1, 1.0 - saturate(dist / _GlobalInteractorParams.x));
                    float3 pushDir = normalize(positionWS - _GlobalInteractorPos);
                    pushDir.y = -0.5;
                    positionWS += pushDir * push * _GlobalInteractorParams.y * input.uv.y;
                #else
                    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                #endif

                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target {
                half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                clip(a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "OcclusionMask"
            Tags { "LightMode" = "OcclusionMask" } // Custom tag identified by Manager
            ColorMask R
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:Setup
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct GrassInstance { float3 position; float rotY; float2 scale; uint colorSeed; float padding; };
            StructuredBuffer<GrassInstance> _VisibleBuffer;
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST; half _Cutoff; 
            float _WindSpeed, _WindStrength, _WindFreq;
            float3 _GlobalInteractorPos; float4 _GlobalInteractorParams;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            void Setup() {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                GrassInstance data = _VisibleBuffer[unity_InstanceID];
                unity_ObjectToWorld = 0;
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(data.position, 1);
                unity_ObjectToWorld._m00_m11_m22 = float3(1, 1, 1);
                unity_WorldToObject = unity_ObjectToWorld;
                #endif
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                float3 pos = input.positionOS.xyz;

                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    GrassInstance data = _VisibleBuffer[unity_InstanceID];
                    pos.x *= data.scale.x; pos.y *= data.scale.y;
                    float s, c; sincos(data.rotY * 0.0174532925, s, c);
                    float3 rotPos; rotPos.x = pos.x * c + pos.z * s; rotPos.y = pos.y; rotPos.z = pos.z * c - pos.x * s;
                    float3 positionWS = rotPos + data.position;
                    
                    float2 windUV = positionWS.xz * _WindFreq + _Time.y * _WindSpeed;
                    float windNoise = sin(windUV.x + windUV.y + data.colorSeed);
                    positionWS += float3(windNoise, 0, windNoise) * _WindStrength * input.uv.y;

                    float dist = distance(positionWS.xz, _GlobalInteractorPos.xz);
                    float push = smoothstep(0, 1, 1.0 - saturate(dist / _GlobalInteractorParams.x));
                    float3 pushDir = normalize(positionWS - _GlobalInteractorPos);
                    pushDir.y = -0.5;
                    positionWS += pushDir * push * _GlobalInteractorParams.y * input.uv.y;
                #else
                    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                #endif

                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target {
                half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                clip(a - _Cutoff);
                return half4(1, 0, 0, 1);
            }
            ENDHLSL
        }
    }
}