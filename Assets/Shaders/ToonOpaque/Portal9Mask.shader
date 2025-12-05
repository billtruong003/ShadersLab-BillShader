Shader "Stylized/PortalFrame9Mask_Toon_DirRim"
{
    Properties
    {
        [Header(Mask System)]
        [NoScaleOffset] _MaskMap("Mask Map (RGBA Point)", 2D) = "black"{}

        [Header(Toon Shading)]
        _RampThreshold("Ramp Threshold", Range(0, 1)) = 0.5
        _RampSmoothness("Ramp Smoothness", Range(0.001, 0.5)) = 0.01

        [Header(Directional Rim Light)]
        [HDR] _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimSize("Rim Size", Range(0, 1)) = 0.2
        _RimSmooth("Rim Smoothness", Range(0.001, 0.5)) = 0.01

        [Header(Region Colors)]
        _ColUntinted("Untinted (Black A0)", Color) = (0.2, 0.2, 0.2, 1)
        _ColExtra("Extra (Black A1)", Color) = (0.5, 0.5, 0.5, 1)
        [HDR] _ColRed("Red Region", Color) = (1, 0, 0, 1)
        [HDR] _ColGreen("Green Region", Color) = (0, 1, 0, 1)
        [HDR] _ColBlue("Blue Region", Color) = (0, 0, 1, 1)
        [HDR] _ColCyan("Cyan Region", Color) = (0, 1, 1, 1)
        [HDR] _ColMagenta("Magenta Region", Color) = (1, 0, 1, 1)
        [HDR] _ColYellow("Yellow Region", Color) = (1, 1, 0, 1)

        [Header(Emission)]
        [HDR] _EmissionColor("Emission (White Region)", Color) = (1, 1, 1, 1)
        _EmissionPower("Emission Power", Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : NORMAL;
                float3 viewDirWS : TEXCOORD1;
                float4 shadowCoord : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            TEXTURE2D(_MaskMap);
            SAMPLER(sampler_MaskMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _MaskMap_ST;
            float4 _ColUntinted;
            float4 _ColExtra;
            float4 _ColRed;
            float4 _ColGreen;
            float4 _ColBlue;
            float4 _ColCyan;
            float4 _ColMagenta;
            float4 _ColYellow;
            float4 _EmissionColor;
            float4 _RimColor;
            float _EmissionPower;
            float _RampThreshold;
            float _RampSmoothness;
            float _RimSize;
            float _RimSmooth;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MaskMap);
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.shadowCoord = GetShadowCoord(vertexInput);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half3 CalculateToonLighting(Light light, float3 normalWS, float3 viewDirWS, float3 albedo)
            {
                float3 lightDir = normalize(light.direction);
                float shadowAtten = light.shadowAttenuation * light.distanceAttenuation;
                float NdotL = saturate(dot(normalWS, lightDir));

                float rampPos = _RampThreshold;
                float rampSoft = _RampSmoothness;
                float toonDiff = smoothstep(rampPos - rampSoft, rampPos + rampSoft, NdotL);
                float3 diffuse = toonDiff * light.color * shadowAtten;

                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                float rimBase = fresnel * NdotL;
                float rimThresh = 1.0 - _RimSize;
                float rimIntensity = smoothstep(rimThresh, rimThresh + _RimSmooth, rimBase);
                float3 rim = rimIntensity * _RimColor.rgb * light.color * shadowAtten;

                return (diffuse + rim) * albedo;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float4 maskSample = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);
                float3 m = step(0.5, maskSample.rgb);
                float mAlpha = step(0.5, maskSample.a);

                float isRed = m.r * (1 - m.g) * (1 - m.b);
                float isGreen = (1 - m.r) * m.g * (1 - m.b);
                float isBlue = (1 - m.r) * (1 - m.g) * m.b;
                float isCyan = (1 - m.r) * m.g * m.b;
                float isMagenta = m.r * (1 - m.g) * m.b;
                float isYellow = m.r * m.g * (1 - m.b);
                float isWhite = m.r * m.g * m.b;
                float isBlack = (1 - m.r) * (1 - m.g) * (1 - m.b);

                float isExtra = isBlack * mAlpha;
                float isUntinted = isBlack * (1 - mAlpha);

                half4 tint = isUntinted * _ColUntinted +
                isExtra * _ColExtra +
                isRed * _ColRed +
                isGreen * _ColGreen +
                isBlue * _ColBlue +
                isCyan * _ColCyan +
                isMagenta * _ColMagenta +
                isYellow * _ColYellow +
                isWhite * float4(1, 1, 1, 1);

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                Light mainLight = GetMainLight(input.shadowCoord);
                half3 finalColor = CalculateToonLighting(mainLight, normalWS, viewDirWS, tint.rgb);

                int pixelLightCount = GetAdditionalLightsCount();
                for (int i = 0;
                i < pixelLightCount;
                ++i)
                {
                    Light addLight = GetAdditionalLight(i, input.positionCS);
                    finalColor += CalculateToonLighting(addLight, normalWS, viewDirWS, tint.rgb);
                }

                half3 ambient = SampleSH(normalWS) * tint.rgb;
                half3 emission = isWhite * _EmissionColor.rgb * _EmissionPower;

                finalColor += ambient + emission;
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ColorMask 0

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = GetShadowCasterPositionCS(vertexInput.positionWS, input.normalOS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
