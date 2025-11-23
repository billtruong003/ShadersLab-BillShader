Shader "Stylized/OptimizedSun"
{
    Properties
    {
        [Header(Textures)]
        _NoiseTex("Noise Texture", 2D) = "white" {}

        [Header(Colors)]
        [MainColor] _CoreColor("Core Color", Color) = (1, 0.9, 0.4, 1)
        [HDR] _RimColor("Rim Color", Color) = (1, 0.5, 0, 1)
        _SunBrightness("Brightness", Range(1, 10)) = 1.5

        [Header(Animation)]
        _ScrollSpeedPrimary("Primary Scroll Speed", Vector) = (0.1, 0.5, 0, 0)
        _ScrollSpeedSecondary("Secondary Scroll Speed", Vector) = (-0.2, 0.1, 0, 0)

        [Header(Displacement)]
        _VertexDisplacement("Displacement Strength", Range(0, 1)) = 0.1

        [Header(Fresnel)]
        _FresnelPower("Fresnel Power", Range(0, 10)) = 3.0
        _FresnelBias("Fresnel Bias", Range(0, 1)) = 0.2
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

        Pass
        {
            Name "StylizedSunPass"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _RimColor;
                float4 _NoiseTex_ST;
                float4 _ScrollSpeedPrimary;
                float4 _ScrollSpeedSecondary;
                float _SunBrightness;
                float _VertexDisplacement;
                float _FresnelPower;
                float _FresnelBias;
            CBUFFER_END

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

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
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half GetNoise(float2 uv, float time)
            {
                float2 uv1 = uv + (time * _ScrollSpeedPrimary.xy);
                float2 uv2 = uv + (time * _ScrollSpeedSecondary.xy);

                half noise1 = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, uv1, 0).r;
                half noise2 = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, uv2, 0).r;

                return (noise1 + noise2) * 0.5;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float2 uv = TRANSFORM_TEX(input.uv, _NoiseTex);
                output.uv = uv;

                half noiseVal = GetNoise(uv, _Time.y);
                
                float3 displacedPos = input.positionOS.xyz + (input.normalOS * noiseVal * _VertexDisplacement);

                output.positionWS = TransformObjectToWorld(displacedPos);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 viewDir = GetWorldSpaceViewDir(input.positionWS);
                float3 normal = normalize(input.normalWS);
                float NdotV = saturate(dot(normal, viewDir));

                half noiseVal = GetNoise(input.uv, _Time.y);

                half fresnel = pow(1.0 - NdotV, _FresnelPower);
                fresnel = saturate(fresnel + _FresnelBias);

                half3 sunColor = lerp(_CoreColor.rgb, _RimColor.rgb, noiseVal);
                half3 finalColor = lerp(sunColor, _RimColor.rgb, fresnel) * _SunBrightness;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}