Shader "CleanCode/InteractiveGrassDetail"
{
    Properties
    {
        [Header(Base Settings)]
        [MainTexture] _BaseMap("Base Map (Alpha)", 2D) = "white"{}
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _GroundBlend("Ground Blend Height", Range(0.001, 1.0)) = 0.2

        [Header(Color Gradient)]
        [HDR] [MainColor] _TopColor("Top Color", Color) = (0.4, 0.8, 0.4, 1)
        [HDR] _BottomColor("Bottom Color", Color) = (0.1, 0.3, 0.1, 1)

        [Header(Detail Blending)]
        _DetailMap("Detail Noise (World Space)", 2D) = "white"{}
        [HDR] _DetailColor("Detail Tint", Color) = (0.8, 0.8, 0.8, 1)
        _DetailScale("Detail Scale", Float) = 0.5
        _DetailBlend("Detail Intensity", Range(0.0, 1.0)) = 0.5

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
        ZTest LEqual
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "FoliageForward"
            }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _TopColor;
            half4 _BottomColor;
            half4 _DetailColor;
            half4 _TranslucencyColor;
            half4 _EmissionColor;
            half _Cutoff;
            half _GroundBlend;
            half _DetailScale;
            half _DetailBlend;
            half _TranslucencyGain;
            half _TranslucencyDistortion;
            half _TranslucencyPower;
            half _WindSpeed;
            half _WindStrength;
            half _InteractionRadius;
            half _InteractionStrength;
            CBUFFER_END

            float3 _GlobalInteractorPos;

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_DetailMap);
            SAMPLER(sampler_DetailMap);

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                float wind = sin(_Time.y * _WindSpeed + positionWS.x + positionWS.z) * _WindStrength * input.uv.y;
                positionWS.x += wind;
                positionWS.z += wind;

                float3 dir = positionWS - _GlobalInteractorPos;
                dir.y = 0;
                float dist = length(dir);
                float influence = saturate(1.0 - dist / _InteractionRadius);
                float3 push = normalize(dir) * influence * influence * _InteractionStrength * input.uv.y;

                positionWS += push;
                positionWS.y -= length(push) * 0.5;

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
                half blendFactor = smoothstep(0.0, _GroundBlend, input.uv.y);
                clip((baseMap.a * blendFactor) - _Cutoff);

                half3 gradientColor = lerp(_BottomColor.rgb, _TopColor.rgb, input.uv.y);
                float2 detailUV = input.positionWS.xz * _DetailScale;
                half4 detailSample = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, detailUV);
                half3 blendedColor = gradientColor * lerp(half3(1, 1, 1), detailSample.rgb * _DetailColor.rgb, _DetailBlend);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 N = normalize(input.normalWS);
                half3 L = normalize(mainLight.direction);
                half3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);

                half NdotL = saturate(dot(N, L));

                half3 backLitDir = L + (N * _TranslucencyDistortion);
                half transDot = saturate(dot(V, -backLitDir));
                half3 translucency = _TranslucencyGain * pow(transDot, _TranslucencyPower) * _TranslucencyColor.rgb;

                half3 ambient = SampleSH(N);
                half3 finalColor = blendedColor * (ambient + (mainLight.color * (NdotL + translucency) * mainLight.shadowAttenuation));

                return half4(finalColor + _EmissionColor.rgb, 1.0);
            }
            ENDHLSL
        }

        UsePass "CleanCode/InteractiveGrass/ShadowCaster"
    }
}
