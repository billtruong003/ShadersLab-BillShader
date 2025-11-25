Shader "Unlit/URP_CircularHealthBar_Occluder"
{
    Properties
    {
        [Header(Fill and Shape)]
        _FillAmount ("Fill Amount", Range(0.0, 1.0)) = 0.5
        _BorderThickness ("Border Thickness", Range(0.0, 0.5)) = 0.05
        _RotationStart("Rotation Start (Degrees)", Range(0, 360)) = 90
        _Cutoff("Alpha Cutoff (For Occlusion)", Range(0.0, 1.0)) = 0.1

        [Header(Colors)]
        _BackgroundColor ("Background Color", Color) = (0.1, 0.1, 0.1, 1)
        _BorderColor ("Border Color", Color) = (1, 1, 1, 1)
        [HDR] _FullColor ("Full Progress Color", Color) = (0.1, 0.8, 0.2, 1)
        [HDR] _LowColor ("Low Progress Color", Color) = (0.8, 0.1, 0.1, 1)

        [Header(Rendering Options)]
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "UniversalForward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite [_ZWrite]
            ZTest [_ZTest]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define PI 3.14159265359

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

            CBUFFER_START(UnityPerMaterial)
            half _FillAmount;
            half _BorderThickness;
            float _RotationStart;
            half _Cutoff;
            half4 _BackgroundColor;
            half4 _BorderColor;
            half4 _FullColor;
            half4 _LowColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 centeredUV = (input.uv - 0.5) * 2.0;
                float dist = length(centeredUV);

                    // Anti-aliasing width based on screen derivatives
                float aaWidth = fwidth(dist);

                    // Shape Masks
                float outerRadius = 1.0;
                float innerRadius = 1.0 - _BorderThickness;

                float shapeMask = 1.0 - smoothstep(outerRadius - aaWidth, outerRadius, dist);
                float contentMask = 1.0 - smoothstep(innerRadius - aaWidth, innerRadius, dist);
                float borderMask = shapeMask - contentMask;

                    // Angle Calculation
                float angle = atan2(centeredUV.y, centeredUV.x);
                float rotationRad = _RotationStart * PI / 180.0;

                    // Normalize angle to 0-1 range based on rotation
                float angleNorm = frac((angle + rotationRad) / (2.0 * PI));

                    // Fill Mask
                float fillMask = smoothstep(0.0, aaWidth * 2.0, _FillAmount - angleNorm);

                    // Color Logic
                half4 progressColor = lerp(_LowColor, _FullColor, saturate(_FillAmount * 1.2));
                half4 contentColor = lerp(_BackgroundColor, progressColor, fillMask);

                half4 finalColor = contentColor * contentMask;
                finalColor = lerp(finalColor, _BorderColor, borderMask);
                finalColor.a *= shapeMask;

                    // Clipping for Outline Occlusion
                    // This ensures only the visible ring writes to the occlusion mask/depth
                clip(finalColor.a - _Cutoff);

                return finalColor;
            }
            ENDHLSL
        }

            // Support for DepthOnly pass ensures this object writes correct depth
            // if ZWrite is On, further helping with sorting and occlusion.
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            CBUFFER_START(UnityPerMaterial)
            half _FillAmount;
            half _BorderThickness;
            half _Cutoff;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float2 centeredUV = (input.uv - 0.5) * 2.0;
                float dist = length(centeredUV);
                float shapeMask = 1.0 - smoothstep(0.99, 1.0, dist);
                clip(shapeMask - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
