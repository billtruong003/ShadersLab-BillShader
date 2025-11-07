    // Toony Stylized Trail Shader for URP
    // Author: AI Assistant
    // Version: 1.0

Shader "Custom/StylizedTrailURP"
{
    Properties
    {
        [Header(Main Settings)]
        _MainTex ("Main Texture (RGBA)", 2D) = "white"{}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(1, 20)) = 1

        [Header(Scrolling Noise and Distortion)]
        _NoiseTex ("Noise Texture (for Distortion & Dissolve)", 2D) = "gray"{}
        _ScrollSpeedX ("Main Texture Scroll Speed X", Float) = 1.0
        _ScrollSpeedY ("Noise Texture Scroll Speed Y", Float) = 0.5
        _DistortionStrength ("Distortion Strength", Range(0, 0.5)) = 0.1

        [Header(Dissolve Effect)]
        _DissolveThreshold ("Dissolve Threshold", Range(0, 1)) = 0.0
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0.01, 0.5)) = 0.1
        _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1, 0.5, 0, 1)

        [Header(Edge Glow or Rim Effect)]
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.1, 10)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _NoiseTex_ST;
            half4 _Color;
            half _Intensity;
            half _ScrollSpeedX;
            half _ScrollSpeedY;
            half _DistortionStrength;
            half _DissolveThreshold;
            half _DissolveEdgeWidth;
            half4 _DissolveEdgeColor;
            half4 _RimColor;
            half _RimPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;

                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;

                output.worldNormal = GetVertexNormalInputs(input.normalOS).normalWS;
                output.viewDir = normalize(GetCameraPositionWS() - positionInputs.positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                    // -- UV Scrolling and Distortion --
                float2 scrolledUV = input.uv;
                scrolledUV.x += _Time.y * _ScrollSpeedX;

                float2 noiseUV = input.uv + _Time.y * _ScrollSpeedY;
                half noiseValue = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                scrolledUV.x += (noiseValue - 0.5) * _DistortionStrength;

                    // -- Main Texture Sampling --
                half4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, scrolledUV);

                    // -- Base Color Calculation --
                    // Multiply texture by vertex color (from Trail Renderer) and the tint color property.
                half4 baseColor = mainTexColor * input.color * _Color;

                    // -- Dissolve Effect --
                half dissolveNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv).r;
                half dissolveAmount = smoothstep(_DissolveThreshold, _DissolveThreshold + _DissolveEdgeWidth, dissolveNoise);

                half3 finalColor = baseColor.rgb;
                half finalAlpha = baseColor.a * (1.0 - dissolveAmount);

                    // Add edge color for dissolve
                if (_DissolveThreshold > 0.0)
                {
                    half edgeFactor = 1.0 - smoothstep(_DissolveThreshold + _DissolveEdgeWidth, _DissolveThreshold + _DissolveEdgeWidth * 2.0, dissolveNoise);
                    finalColor = lerp(finalColor, _DissolveEdgeColor.rgb * _Intensity, (1.0 - dissolveAmount) * edgeFactor);
                }

                    // -- Rim Light / Edge Glow --
                half rim = 1.0 - saturate(dot(input.viewDir, input.worldNormal));
                half rimFalloff = pow(rim, _RimPower);
                half3 rimColor = _RimColor.rgb * rimFalloff * _RimColor.a;

                finalColor += rimColor;

                    // -- Final Composition --
                finalColor *= _Intensity;

                    // Use the trail's original alpha (from vertex color) to fade the trail naturally over its lifetime.
                finalAlpha *= input.color.a;

                    // Clip pixels that are fully dissolved
                clip(finalAlpha - 0.001);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
