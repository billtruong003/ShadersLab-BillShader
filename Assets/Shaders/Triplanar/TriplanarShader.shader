Shader "URP/Toon/Lit Triplanar MinionsArt 2025"
{
    Properties
    {
        _Color ("Main Color", Color) = (1, 1, 1, 1)

        [Header(Textures)]
        _TopTex ("Top Texture (Grass/Snow)", 2D) = "white"{}
        _TopNormal ("Top Normal", 2D) = "bump"{}
        _TopScale ("Top Scale", Float) = 0.1

        _SideTex ("Side/Bottom Texture (Rock)", 2D) = "white"{}
        _SideNormal ("Side Normal", 2D) = "bump"{}
        _SideScale ("Side Scale", Float) = 0.15

        _Noise ("Noise (R = Grass Mask)", 2D) = "gray"{}
        _NoiseScale ("Noise Scale", Float) = 0.08

        [Header(Toon Settings)]
        _Ramp ("Toon Ramp", 2D) = "white"{}
        _AdditionalLightsRampOffset ("Additional Lights Falloff", Range(-1, 1)) = -0.3
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.15

        [Header(Top Spread)]
        _TopSpread ("Top Spread", Range(0, 1.5)) = 0.65
        _GrassPower ("Grass Power", Range(0.1, 10)) = 3
        _EdgeWidth ("Edge Darken Width", Range(0, 0.5)) = 0.08
        _EdgeColor ("Edge Darken Color", Color) = (0.35, 0.35, 0.35, 1)

        [Header(Rim)]
        _RimPower ("Rim Power", Range(0.5, 20)) = 5
        [HDR] _RimColorTop ("Rim Top", Color) = (1, 1, 1, 1)
        [HDR] _RimColorSide ("Rim Side", Color) = (0.8, 0.9, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry"
        }

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
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);
                float4 shadowCoord : TEXCOORD5;
            };

            TEXTURE2D(_TopTex);
            SAMPLER(sampler_TopTex);
            TEXTURE2D(_SideTex);
            SAMPLER(sampler_SideTex);
            TEXTURE2D(_TopNormal);
            SAMPLER(sampler_TopNormal);
            TEXTURE2D(_SideNormal);
            SAMPLER(sampler_SideNormal);
            TEXTURE2D(_Noise);
            SAMPLER(sampler_Noise);
            TEXTURE2D(_Ramp);
            SAMPLER(sampler_Ramp);

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _TopScale, _SideScale, _NoiseScale;
            float _TopSpread, _GrassPower, _EdgeWidth;
            float _AdditionalLightsRampOffset;
            float _AmbientStrength;
            float _RimPower;
            half4 _EdgeColor, _RimColorTop, _RimColorSide;
            CBUFFER_END

                // ── Toon Ramp Function (Minions Art 2025) ──────────────────────
            half3 ToonLighting(Light light, half3 normalWS, half rampOffset = 0)
            {
                half NdotL = dot(normalWS, light.direction);
                half toon = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, half2(saturate(NdotL * 0.5 + 0.5 + rampOffset), 0.5)).r;
                return light.color * toon * light.distanceAttenuation * light.shadowAttenuation;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs norm = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS = norm.normalWS;
                OUT.viewDirWS = GetWorldSpaceViewDir(pos.positionWS);
                OUT.shadowCoord = GetShadowCoord(pos);

                OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, OUT.lightmapUV);
                OUTPUT_SH(OUT.normalWS, OUT.vertexSH);

                float sign = IN.tangentOS.w * GetOddNegativeScale();
                float3 bitangentWS = cross(norm.normalWS, norm.tangentWS.xyz) * sign;
                    // We don't need full TBN for final normal here because we rebuild it in frag
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 pos = IN.positionWS;
                float3 V = normalize(IN.viewDirWS);
                float3 N = normalize(IN.normalWS);

                    // ── Triplanar Blending (sharp & correct) ─────────────────────
                float3 blend = pow(abs(N), 4);
                blend /= (blend.x + blend.y + blend.z + 0.0001);

                    // Color
                half3 topCol = SAMPLE_TEXTURE2D(_TopTex, sampler_TopTex, pos.xy * _TopScale) * blend.z +
                SAMPLE_TEXTURE2D(_TopTex, sampler_TopTex, pos.zy * _TopScale) * blend.x +
                SAMPLE_TEXTURE2D(_TopTex, sampler_TopTex, pos.zx * _TopScale) * blend.y;

                half3 sideCol = SAMPLE_TEXTURE2D(_SideTex, sampler_SideTex, pos.xy * _SideScale) * blend.z +
                SAMPLE_TEXTURE2D(_SideTex, sampler_SideTex, pos.zy * _SideScale) * blend.x +
                SAMPLE_TEXTURE2D(_SideTex, sampler_SideTex, pos.zx * _SideScale) * blend.y;

                    // Normal triplanar
                half3 topN = UnpackNormal(SAMPLE_TEXTURE2D(_TopNormal, sampler_TopNormal, pos.xy * _TopScale)) * blend.z +
                UnpackNormal(SAMPLE_TEXTURE2D(_TopNormal, sampler_TopNormal, pos.zy * _TopScale)) * blend.x +
                UnpackNormal(SAMPLE_TEXTURE2D(_TopNormal, sampler_TopNormal, pos.zx * _TopScale)) * blend.y;

                half3 sideN = UnpackNormal(SAMPLE_TEXTURE2D(_SideNormal, sampler_SideNormal, pos.xy * _SideScale)) * blend.z +
                UnpackNormal(SAMPLE_TEXTURE2D(_SideNormal, sampler_SideNormal, pos.zy * _SideScale)) * blend.x +
                UnpackNormal(SAMPLE_TEXTURE2D(_SideNormal, sampler_SideNormal, pos.zx * _SideScale)) * blend.y;

                    // Grass spread using noise
                half noise = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, pos.xz * _NoiseScale).r;
                half grassMask = pow(saturate(N.y + (noise - 0.5) * 0.8), _GrassPower);
                half heightMask = N.y + grassMask * 0.6;
                half topMask = saturate((heightMask - _TopSpread) * 20);
                half edgeMask = saturate((heightMask - (_TopSpread - _EdgeWidth)) * 20);

                half3 albedo = lerp(sideCol, topCol, topMask);
                albedo = lerp(albedo * _EdgeColor.rgb, albedo, 1 - edgeMask) * _Color.rgb;

                half3 finalNormal = normalize(lerp(sideN, topN, topMask));

                    // ── Lighting (Minions Art 2025 version) ─────────────────────
                half3 litColor = 0;

                    // Main Directional Light
                Light mainLight = GetMainLight(IN.shadowCoord);
                litColor += ToonLighting(mainLight, finalNormal, 0);

                    // Additional Lights (Point + Spot) – Unity 6 compatible
                #ifdef _ADDITIONAL_LIGHTS
                    uint pixelLightCount = GetAdditionalLightsCount();

                    LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, pos, half4(1, 1, 1, 1));
                    litColor += ToonLighting(light, finalNormal, _AdditionalLightsRampOffset);
                    LIGHT_LOOP_END
                #endif

                    // Ambient + Vertex SH (works even with no directional light)
                half3 ambient = SampleSH(finalNormal) * _AmbientStrength;
                litColor += ambient;

                    // Rim
                half rim = pow(1 - saturate(dot(V, finalNormal)), _RimPower);
                half3 rimCol = lerp(_RimColorSide.rgb, _RimColorTop.rgb, topMask) * rim;
                litColor += rimCol;

                half3 final = albedo * litColor;

                return half4(final, 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
