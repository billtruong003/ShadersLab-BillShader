Shader "Universal Render Pipeline/UI/TextMeshPro_SciFiFire_Optimized"
{
    Properties
    {
        [MainTexture] _MainTex ("Font Atlas", 2D) = "white"{}
        _NoiseTex ("Noise Texture", 2D) = "black"{}

        [Header(Fire Settings)]
        [HDR] _CoreColor ("Core Color", Color) = (1, 0.9, 0.5, 1)
        [HDR] _EdgeColor ("Edge Color", Color) = (1, 0.2, 0, 1)
        _FireSpeed ("Fire Speed (X, Y)", Vector) = (0.2, 0.5, 0, 0)
        _Distortion ("Distortion Strength", Range(0, 0.1)) = 0.02
        _NoiseScale ("Noise Scale", Float) = 1.0

        [Header(Text Settings)]
        _Smoothness ("Font Smoothness", Range(0, 1)) = 0.5
        _OutlineWidth ("Outline Width", Range(0, 1)) = 0.2
        _FaceDilate ("Face Dilate", Range(-1, 1)) = 0.0

        [Header(System)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "SciFiFire"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 noiseUV : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _NoiseTex_ST;
            float4 _CoreColor;
            float4 _EdgeColor;
            float4 _FireSpeed;
            float4 _ClipRect;
            float _Distortion;
            float _NoiseScale;
            float _Smoothness;
            float _OutlineWidth;
            float _FaceDilate;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                float2 worldPosXY = vertexInput.positionWS.xy * _NoiseScale;
                float2 scroll = _FireSpeed.xy * _Time.y;
                output.noiseUV = worldPosXY - scroll;

                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half2 noiseUV1 = input.noiseUV;
                half2 noiseUV2 = input.noiseUV * 0.5 + _Time.x * 0.2;

                half noise1 = tex2D(_NoiseTex, noiseUV1).r;
                half noise2 = tex2D(_NoiseTex, noiseUV2).g;
                half finalNoise = (noise1 + noise2) * 0.5;

                float2 distortedUV = input.uv + (finalNoise * _Distortion - (_Distortion * 0.5));

                half sdf = tex2D(_MainTex, distortedUV).a;

                half distanceChange = _FaceDilate * 0.5;
                half sdfBase = sdf + distanceChange;

                half alpha = smoothstep(0.5 - _Smoothness, 0.5 + _Smoothness, sdfBase);

                half firePattern = smoothstep(0.3, 0.8, finalNoise * sdfBase);
                half4 emissionColor = lerp(_EdgeColor, _CoreColor, firePattern + (sdfBase * 0.5));

                half4 finalColor = emissionColor * input.color;
                finalColor.a *= alpha;

                finalColor.a *= step(input.positionCS.x, _ClipRect.z) * step(_ClipRect.x, input.positionCS.x) *
                step(input.positionCS.y, _ClipRect.w) * step(_ClipRect.y, input.positionCS.y);

                clip(finalColor.a - 0.01);

                return finalColor;
            }
            ENDHLSL
        }
    }
}
