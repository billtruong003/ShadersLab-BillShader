Shader "Custom/Toon/LiquidProgressBar_URP"
{
    Properties
    {
        [Header(Liquid Settings)]
        _FillAmount("Fill Amount", Range(0, 1)) = 0.5
        _LiquidColor("Liquid Color", Color) = (0.3, 0.7, 1, 1)
        _SurfaceColor("Top Surface Color", Color) = (0.6, 0.9, 1, 1)
        
        [Header(Mesh Settings)]
        _MinY("Mesh Min Y", Float) = -1.0
        _MaxY("Mesh Max Y", Float) = 1.0
        
        [Header(Stylization)]
        _SurfaceThickness("Surface Line Thickness", Range(0, 0.1)) = 0.05
        _WobbleFrequency("Wobble Frequency", Float) = 1.0
        _WobbleAmplitude("Wobble Amplitude", Float) = 0.05
        _WobbleSpeed("Wobble Speed", Float) = 1.0

        [Header(Toon Lighting)]
        _RampThreshold("Ramp Threshold", Range(0, 1)) = 0.5
        _RampSmoothness("Ramp Smoothness", Range(0, 1)) = 0.05
        
        [Header(Rim)]
        _RimColor("Rim Color", Color) = (1, 1, 1, 0.5)
        _RimPower("Rim Power", Range(0.1, 10)) = 3.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off 

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _LiquidColor;
                half4 _SurfaceColor;
                half4 _RimColor;
                float _FillAmount;
                float _MinY;
                float _MaxY;
                float _SurfaceThickness;
                float _WobbleFrequency;
                float _WobbleAmplitude;
                float _WobbleSpeed;
                float _RampThreshold;
                float _RampSmoothness;
                float _RimPower;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionOS = input.positionOS.xyz;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                return output;
            }

            half4 Frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                float wobble = sin((input.positionOS.x + input.positionOS.z) * _WobbleFrequency + _Time.y * _WobbleSpeed) * _WobbleAmplitude;
                float currentFillHeight = lerp(_MinY, _MaxY, _FillAmount) + wobble;

                if (input.positionOS.y > currentFillHeight)
                {
                    discard;
                }

                if (!isFrontFace)
                {
                    return half4(_SurfaceColor.rgb, _LiquidColor.a);
                }

                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                Light mainLight = GetMainLight();
                float NdotL = dot(normalWS, mainLight.direction);
                float d = NdotL * 0.5 + 0.5;
                float ramp = smoothstep(_RampThreshold, _RampThreshold + _RampSmoothness, d * (mainLight.shadowAttenuation * mainLight.distanceAttenuation));
                
                half3 litColor = _LiquidColor.rgb * (ramp * mainLight.color);
                
                // Add Ambient
                litColor += _LiquidColor.rgb * 0.3; 

                // Rim Light
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float rimIntensity = pow(1.0 - NdotV, _RimPower);
                half3 rim = rimIntensity * _RimColor.rgb * _RimColor.a;

                // Top Surface Band (Foam Line)
                float distToSurface = currentFillHeight - input.positionOS.y;
                float surfaceLine = 1.0 - smoothstep(0.0, _SurfaceThickness, distToSurface);
                
                half3 finalColor = lerp(litColor + rim, _SurfaceColor.rgb, surfaceLine);

                return half4(finalColor, _LiquidColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
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
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float _FillAmount;
                float _MinY;
                float _MaxY;
                float _WobbleFrequency;
                float _WobbleAmplitude;
                float _WobbleSpeed;
            CBUFFER_END

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                output.positionOS = input.positionOS.xyz;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float wobble = sin((input.positionOS.x + input.positionOS.z) * _WobbleFrequency + _Time.y * _WobbleSpeed) * _WobbleAmplitude;
                float currentFillHeight = lerp(_MinY, _MaxY, _FillAmount) + wobble;

                if (input.positionOS.y > currentFillHeight)
                {
                    discard;
                }

                return float4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
}