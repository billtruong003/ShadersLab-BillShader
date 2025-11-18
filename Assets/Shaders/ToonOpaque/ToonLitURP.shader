    // ToonLitFull.hlsl
    // Unity URP - Full HLSL Toon Shader (dựa hoàn toàn logic Minions Art)
    // Support: Main Light + Additional Lights + Shadows + Rim + Emissive Mask

Shader "Custom/Toon Lit Full (HLSL)"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white"{}
        _ShadowSteps ("Shadow Steps (1-3)", Range(1, 3)) = 2
        _ShadowThreshold ("Shadow Threshold", Range(-1, 1)) = 0.0
        _ShadowSoftness ("Shadow Softness", Range(0.01, 1)) = 0.1

        _RimPower ("Rim Power", Range(0.1, 20)) = 5
        _RimStrength ("Rim Strength", Range(0, 20)) = 4
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)

        _EmissiveMask ("Emissive Mask (R = Unlit)", 2D) = "black"{}
        _EmissiveStrength ("Emissive Strength", Range(0, 10)) = 2

        _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0.5, 0.5, 0.6, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;   // lightmap UV
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
                float4 shadowCoord : TEXCOORD6;
                float fogFactor : TEXCOORD7;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_EmissiveMask);
            SAMPLER(sampler_EmissiveMask);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half _ShadowSteps;
            half _ShadowThreshold;
            half _ShadowSoftness;
            half _RimPower;
            half _RimStrength;
            half4 _RimColor;
            half4 _HighlightColor;
            half4 _ShadowColor;
            half _EmissiveStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normInputs.normalWS;
                OUT.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.lightmapUV = IN.uv2.xy * unity_LightmapST.xy + unity_LightmapST.zw;

                OUTPUT_LIGHTMAP_UV(IN.uv2, unity_LightmapST, OUT.lightmapUV);
                OUTPUT_SH(OUT.normalWS, OUT.vertexSH);

                OUT.shadowCoord = GetShadowCoord(posInputs);
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                    // Textures
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half emissiveMask = SAMPLE_TEXTURE2D(_EmissiveMask, sampler_EmissiveMask, IN.uv).r;

                    // Lighting setup
                Light mainLight = GetMainLight(IN.shadowCoord);
                half3 lightDir = normalize(mainLight.direction);
                half3 normalWS = normalize(IN.normalWS);
                half3 viewDir = normalize(IN.viewDirWS);

                half NdotL = dot(normalWS, lightDir);
                half shadowAtten = mainLight.shadowAttenuation;   // 0 = full shadow, 1 = lit
                half distanceAtten = mainLight.distanceAttenuation;

                    // === TOON SHADING (Minions Art style) ===
                half toonRamp = saturate((NdotL + _ShadowThreshold) / _ShadowSoftness);
                toonRamp = saturate(toonRamp + shadowAtten - 1);    // combine with real shadow
                toonRamp = floor(toonRamp * _ShadowSteps) / (_ShadowSteps - 0.5); // 2-3 steps

                half3 baseColor = lerp(_ShadowColor.rgb, _HighlightColor.rgb, toonRamp);
                half3 litColor = baseColor * mainLight.color;

                    // Additional lights (toon style)
                #ifdef _ADDITIONAL_LIGHTS
                    uint pixelLightCount = GetAdditionalLightsCount();
                    for (uint i = 0;
                    i < pixelLightCount;
                    ++i)
                    {
                        Light light = GetAdditionalLight(i, IN.positionWS);
                        half3 attenLightDir = normalize(light.direction);
                        half attenNdotL = saturate(dot(normalWS, attenLightDir)) * light.distanceAttenuation * light.shadowAttenuation;
                        half attenToon = step(0.5, attenNdotL);   // hard additional lights
                        litColor += attenToon * light.color * 0.5;
                    }
                #endif

                    // === RIM LIGHT (chỉ hiện ở mặt sáng + bị che bởi shadow) ===
                half rim = 1.0 - saturate(dot(viewDir, normalWS));
                rim = pow(rim, _RimPower);
                half rimFacingLight = saturate(dot(normalWS, lightDir));    // chỉ hiện mặt sáng
                half rimInShadow = saturate(shadowAtten); // không hiện trong bóng đổ
                half finalRim = rim * rimFacingLight * rimInShadow;
                finalRim = step(0.01, finalRim) * finalRim;   // cutoff nhẹ
                half3 rimColor = finalRim * _RimColor.rgb * _RimStrength * mainLight.color;

                    // Final albedo
                half3 albedo = mainTex.rgb * litColor + rimColor;

                    // Emissive / Unlit mask (Red channel)
                half3 emissive = mainTex.rgb * emissiveMask * _EmissiveStrength;
                albedo = lerp(albedo, mainTex.rgb, emissiveMask); // unlit parts
                albedo += emissive;

                    // SH + Lightmap + Fog
                half3 bakedGI = SampleSH(IN.normalWS);
                #ifdef LIGHTMAP_ON
                    bakedGI = SampleLightmap(IN.lightmapUV, normalWS);
                #endif

                albedo = MixFog(albedo + bakedGI * mainTex.rgb, IN.fogFactor);

                return half4(albedo, mainTex.a);
            }
            ENDHLSL
        }

            // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On ZTest LEqual

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowPassVertex(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 clipPos = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                OUT.positionCS = clipPos;
                return OUT;
            }

            half4 ShadowPassFragment(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
