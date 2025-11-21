Shader "Stylized/ProceduralSun"
{
    Properties
    {
        [Header(Colors)]
        [MainColor] _CoreColor("Core Color", Color) = (1, 0.9, 0.4, 1)
        [HDR] _RimColor("Rim Color", Color) = (1, 0.5, 0, 1)
        _SunBrightness("Brightness", Range(1, 10)) = 1.5

        [Header(Noise Configuration)]
        _NoiseScale("Noise Scale", Float) = 5.0
        _NoiseSpeed("Animation Speed", Float) = 1.0
        _NoiseDetail("Noise Detail", Range(1, 5)) = 3

        [Header(Displacement)]
        _VertexDisplacement("Displacement Strength", Range(0, 1)) = 0.1
        _Turbulence("Turbulence", Range(0, 2)) = 1.0

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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _CoreColor;
            half4 _RimColor;
            float _SunBrightness;
            float _NoiseScale;
            float _NoiseSpeed;
            float _NoiseDetail;
            float _VertexDisplacement;
            float _Turbulence;
            float _FresnelPower;
            float _FresnelBias;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            float3 hash33(float3 p)
            {
                p = float3(dot(p, float3(127.1, 311.7, 74.7)),
                dot(p, float3(269.5, 183.3, 246.1)),
                dot(p, float3(113.5, 271.9, 124.6)));
                return - 1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(lerp(dot(hash33(i + float3(0, 0, 0)), f - float3(0, 0, 0)),
                dot(hash33(i + float3(1, 0, 0)), f - float3(1, 0, 0)), u.x),
                lerp(dot(hash33(i + float3(0, 1, 0)), f - float3(0, 1, 0)),
                dot(hash33(i + float3(1, 1, 0)), f - float3(1, 1, 0)), u.x), u.y),
                lerp(lerp(dot(hash33(i + float3(0, 0, 1)), f - float3(0, 0, 1)),
                dot(hash33(i + float3(1, 0, 1)), f - float3(1, 0, 1)), u.x),
                lerp(dot(hash33(i + float3(0, 1, 1)), f - float3(0, 1, 1)),
                dot(hash33(i + float3(1, 1, 1)), f - float3(1, 1, 1)), u.x), u.y), u.z);
            }

            float fbm(float3 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 0.0;

                for (int i = 0;
                i < 4;
                i++)
                {
                    if (i >= _NoiseDetail) break;
                        value += amplitude * noise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float time = _Time.y * _NoiseSpeed;
                float3 noisePos = input.positionOS.xyz * _NoiseScale + time;
                float displacement = fbm(noisePos) * _VertexDisplacement;

                float3 displacedPos = input.positionOS.xyz + (input.normalOS * displacement);

                output.positionWS = TransformObjectToWorld(displacedPos);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = GetWorldSpaceViewDir(input.positionWS);
                float3 normal = normalize(input.normalWS);
                float NdotV = saturate(dot(normal, viewDir));

                float time = _Time.y * _NoiseSpeed;
                float3 noiseCoord = input.positionWS * _Turbulence + time;

                float noiseVal1 = fbm(noiseCoord * _NoiseScale);
                float noiseVal2 = fbm(noiseCoord * _NoiseScale * 2.0 - time * 0.5);
                float combinedNoise = saturate((noiseVal1 + noiseVal2) * 0.5 + 0.5);

                half fresnel = pow(1.0 - NdotV, _FresnelPower);
                fresnel = saturate(fresnel + _FresnelBias);

                half3 sunColor = lerp(_CoreColor.rgb, _RimColor.rgb, combinedNoise);
                half3 finalColor = lerp(sunColor, _RimColor.rgb, fresnel) * _SunBrightness;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
