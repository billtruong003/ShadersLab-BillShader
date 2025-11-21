Shader "URP/VFX/AdvancedEnergyPulse_Pro"
{
    Properties
    {
        [Header(Base Settings)]
        _MainTex ("Pattern Texture", 2D) = "white"{}
        [HDR] _BaseColor ("Base Color", Color) = (0, 0.5, 1, 1)
        _EnergyIntensity ("Energy Intensity", Range(0, 20)) = 2.0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Header(Flow Settings)]
        [Toggle(_USE_WORLD_SPACE)] _UseWorldSpace ("Use World Space", Float) = 0
        [Toggle(_USE_GRAYSCALE_FLOW)] _UseGrayscaleFlow ("Use Grayscale Flow", Float) = 0
        _FlowDirection ("Flow Direction (XYZ)", Vector) = (1, 0, 0, 0)
        _FlowSpeed ("Flow Speed", Float) = 1.0

        [Header(Pulse Shape)]
        _PulseDensity ("Pulse Density", Float) = 5.0
        _PulseWidth ("Pulse Width", Range(0.01, 1.0)) = 0.2
        _PulseSoftness ("Pulse Softness", Range(0.001, 1.0)) = 0.1

        [Header(Color Grading)]
        [Toggle(_USE_RAMP_TEXTURE)] _UseRamp ("Use Ramp Texture", Float) = 0
        _RampTex ("Ramp Gradient", 2D) = "white"{}

        [Header(Surface Options)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dest Blend", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
        [Toggle] _ZWrite ("ZWrite", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _BaseColor;
        float4 _FlowDirection;
        float _EnergyIntensity;
        float _FlowSpeed;
        float _PulseDensity;
        float _PulseWidth;
        float _PulseSoftness;
        float _Cutoff;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_RampTex);
        SAMPLER(sampler_RampTex);
        ENDHLSL

            // ------------------------------------------------------------------
            // PASS 1: Universal Forward (The Main Visuals)
            // ------------------------------------------------------------------
        Pass
        {
            Name "EnergyPulse"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend [_SrcBlend] [_DstBlend]
            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _USE_WORLD_SPACE
            #pragma shader_feature_local _USE_GRAYSCALE_FLOW
            #pragma shader_feature_local _USE_RAMP_TEXTURE

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 vertexColor : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.vertexColor = input.color;
                return output;
            }

            float GetPulseWave(float phase, float width, float softness)
            {
                float wave = frac(phase);
                float center = 0.5;
                float halfWidth = width * 0.5;
                return smoothstep(center - halfWidth - softness, center - halfWidth, wave) *
                (1.0 - smoothstep(center + halfWidth, center + halfWidth + softness, wave));
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 mainTexSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float grayscaleValue = mainTexSample.r;

                float flowPhase = 0;
                #if defined(_USE_GRAYSCALE_FLOW)
                    flowPhase = grayscaleValue * _PulseDensity - (_Time.y * _FlowSpeed);
                #else
                        float3 flowDir = normalize(_FlowDirection.xyz);
                    #if defined(_USE_WORLD_SPACE)
                        flowPhase = dot(input.positionWS, flowDir) * _PulseDensity - (_Time.y * _FlowSpeed);
                    #else
                            flowPhase = dot(float3(input.uv, 0), flowDir) * _PulseDensity - (_Time.y * _FlowSpeed);
                    #endif
                #endif

                float pulse = GetPulseWave(flowPhase, _PulseWidth, _PulseSoftness);
                float finalEnergy = pulse * grayscaleValue;   // Mask by texture

                half3 finalColor;
                #if defined(_USE_RAMP_TEXTURE)
                    half3 rampCol = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(saturate(finalEnergy), 0.5)).rgb;
                    finalColor = rampCol * _BaseColor.rgb;
                #else
                        finalColor = _BaseColor.rgb * finalEnergy;
                #endif

                finalColor *= _EnergyIntensity * input.vertexColor.rgb;
                float finalAlpha = saturate(finalEnergy * _BaseColor.a * input.vertexColor.a);

                    // Clip for Depth/Shadow consistency if needed (optional for additive)
                    // clip(finalAlpha - _Cutoff);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }

            // ------------------------------------------------------------------
            // PASS 2: DepthOnly (For Shadows, Depth of Field, Soft Particles)
            // ------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                    // If you want the "empty" parts to not write depth, enable cutoff:
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(texColor.r * texColor.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

            // ------------------------------------------------------------------
            // PASS 3: DepthNormals (For SSAO, Decals)
            // ------------------------------------------------------------------
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

                // URP Function to pack normals
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            half4 DepthNormalsFrag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(texColor.r * texColor.a - _Cutoff);

                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            }
            ENDHLSL
        }

            // ------------------------------------------------------------------
            // PASS 4: Meta (For Lightmap Baking & Global Illumination)
            // ------------------------------------------------------------------
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }

            Cull Off

            HLSLPROGRAM
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMeta

                // Required for Meta pass
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _SPECGLOSSMAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

                // We override the standard Meta Fragment to support our procedural emission
            half4 UniversalFragmentMeta(VaryingsMetaInput input) : SV_Target
            {
                MetaInput metaInput;
                metaInput.Albedo = 0; // It's energy, no albedo usually
                metaInput.SpecularColor = 0;

                    // Calculate Emission for baking
                    // NOTE: Baking captures a static snapshot. We bake the "Maximum" intensity
                    // or the base color so the surrounding area glows in the lightmap.
                half3 emission = _BaseColor.rgb * _EnergyIntensity;

                    // Optional: Sample texture to only bake emission where the lines are
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                emission *= texColor.r;

                metaInput.Emission = emission;

                return UniversalFragmentMetaInput(metaInput);
            }
            ENDHLSL
        }
    }

        // Assign the Custom Editor
    CustomEditor "EnergyPulseShaderGUI"
}
