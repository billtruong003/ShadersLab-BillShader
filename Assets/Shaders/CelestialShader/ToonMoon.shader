Shader "Stylized/LitMoon"
{
    Properties
    {
        [Header(Surface)]
        _MainTex("Albedo Map", 2D) = "white" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _EmissionMap("Emission Map (Cities/Lava)", 2D) = "black" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,0)

        [Header(Toon Lighting)]
        _RampThreshold("Ramp Threshold", Range(-1, 1)) = 0.0
        _RampSmoothness("Ramp Smoothness", Range(0.001, 1)) = 0.05
        
        [Header(Colors)]
        [MainColor] _BaseColor("Lit Color", Color) = (1, 1, 0.9, 1)
        _ShadowColor("Shadow Color (Earthshine)", Color) = (0.02, 0.02, 0.05, 1)
        
        [Header(Rim Effect)]
        [HDR] _RimColor("Rim Color", Color) = (0.2, 0.6, 1, 1)
        _RimPower("Rim Power", Range(0.1, 10)) = 4.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "Queue" = "Geometry" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "StylizedLitMoon"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _RimColor;
                half4 _EmissionColor;
                float4 _MainTex_ST;
                float _RampThreshold;
                float _RampSmoothness;
                float _RimPower;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.shadowCoord = GetShadowCoord(vertexInput);

                return output;
            }

            half3 CalculateToonLighting(Light light, float3 normalWS, float3 viewDir, half3 albedo)
            {
                float3 lightDir = normalize(light.direction);
                float NdotL = dot(normalWS, lightDir);
                
                float shadow = light.shadowAttenuation * light.distanceAttenuation;
                float litVal = NdotL * shadow;
                
                float ramp = smoothstep(_RampThreshold, _RampThreshold + _RampSmoothness, litVal);
                
                half3 litColor = albedo * _BaseColor.rgb * light.color * ramp;
                
                return litColor;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = GetWorldSpaceViewDir(input.positionWS);
                
                float3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 TBN = float3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv));
                float3 normalWS = normalize(mul(normalTS, TBN));

                half4 albedoSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 albedo = albedoSample.rgb;

                Light mainLight = GetMainLight(input.shadowCoord);
                half3 directLight = CalculateToonLighting(mainLight, normalWS, viewDir, albedo);

                int pixelLightCount = GetAdditionalLightsCount();
                for (int i = 0; i < pixelLightCount; ++i)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    directLight += CalculateToonLighting(addLight, normalWS, viewDir, albedo);
                }

                float mainLightRamp = smoothstep(_RampThreshold, _RampThreshold + _RampSmoothness, dot(normalWS, mainLight.direction) * mainLight.shadowAttenuation);
                half3 shadowSide = albedo * _ShadowColor.rgb * (1.0 - mainLightRamp);

                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;

                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, _RimPower);
                half3 rim = _RimColor.rgb * fresnel * mainLightRamp; 

                half3 finalColor = directLight + shadowSide + emission + rim;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

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

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}