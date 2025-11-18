Shader "ShadersLab/Toon Lit"
{
    Properties
    {
        [Header(Render States)]
        [Enum(Opaque, 0, Cutout, 1, Transparent, 2)] _RenderMode ("Render Mode", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 0
        [Toggle] _ZWrite ("ZWrite", Float) = 1
        [Enum(Off, 0, Front, 1, Back, 2)] _CullMode ("Culling Mode", Float) = 2

        [Header(Base Properties)]
        _BaseMap("Albedo (RGB) Alpha (A)", 2D) = "white"{}
        [HDR] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump"{}
        _BumpScale("Normal Intensity", Range(0.0, 2.0)) = 1.0

        [Header(Alpha Clipping)]
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Header(Emission)]
        [Toggle(_EMISSION_ON)] _EmissionToggle("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionMap("Emission Map", 2D) = "black"{}

        [Header(Lighting)]
        [Toggle(_FORCE_FAKELIGHT_ON)] _ForceFakeLight("Force Fake Light", Float) = 0
        [Toggle(_FAKELIGHT_ON)] _FakeLightMode("Enable Fake Light Fallback", Float) = 1
        _FakeLightColor("Fake Light Color", Color) = (0.8, 0.8, 0.8, 1)
        _FakeLightDirection("Fake Light Direction", Vector) = (0.5, 0.5, -0.5, 0)
        _AmbientColor("Ambient Color", Color) = (0.5, 0.5, 0.5, 0)
        _MaxBrightness("Max Brightness", Range(0, 5)) = 1.5

        [Header(Indirect Lighting)]
        [Toggle(_INDIRECTSPECULAR_ON)] _IndirectSpecular("Enable Environmentreflection", Float) = 0
        _IndirectSpecularIntensity("Reflection Intensity", Range(0, 2)) = 1.0

        [Header(Toon Shading Main Light)]
        [Enum(Smooth, 0, Hard, 1)] _ToonStyle("Style", Float) = 0
        _ShadowTint("Shadow Tint", Color) = (0.1, 0.1, 0.2, 1.0)
        _MidtoneColor("Mid-tone Color", Color) = (0.6, 0.6, 0.6, 1.0)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.4
        _MidtoneThreshold("Mid-tone Threshold", Range(0, 1)) = 0.8
        _ToonRampSmoothness("Ramp Smoothness", Range(0.001, 0.5)) = 0.05

        [Header(Toon Shading Additional Lights)]
        _AddLightShadowTint("Shadow Tint", Color) = (0.2, 0.2, 0.3, 1.0)
        _AddLightMidtoneColor("Mid-tone Color", Color) = (0.7, 0.7, 0.7, 1.0)
        _AddLightShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.1
        _AddLightMidtoneThreshold("Mid-tone Threshold", Range(0, 1)) = 0.6
        _AddLightRampSmoothness("Ramp Smoothness", Range(0.001, 0.5)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull [_CullMode]
            ZWrite [_ZWrite]
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local_fragment _NORMALMAP_ON
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _FORCE_FAKELIGHT_ON
            #pragma shader_feature_local_fragment _FAKELIGHT_ON
            #pragma shader_feature_local_fragment _INDIRECTSPECULAR_ON
            #pragma shader_feature_local_fragment _TOON_STYLE_HARD

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _PROBE_VOLUMES_L1 _PROBE_VOLUMES_L2

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Includes/Toon/ToonLitCore.hlsl"

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs norm = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = norm.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                #ifdef _NORMALMAP_ON
                    output.tangentWS.xyz = norm.tangentWS;
                    output.tangentWS.w = norm.bitangentWS.x;    // lưu bitangent sign hoặc dùng cross sau
                #endif

                return output;
            }

            half4 Frag(Varyings input, half facing : VFACE) : SV_Target
            {
                float3 viewDirWS = GetWorldSpaceViewDir(input.positionWS);
                float3 normalWS = normalize(input.normalWS) * (facing > 0 ? 1 : -1);

                #ifdef _NORMALMAP_ON
                    float3 tangentWS = input.tangentWS.xyz;
                    float3 bitangentWS = cross(input.normalWS, tangentWS) * input.tangentWS.w;
                    normalWS = ApplyNormalMap(input.uv, normalWS, tangentWS, bitangentWS);
                #endif

                half4 albedoAlpha = GetAlbedoAndAlpha(input.uv);
                ApplyAlphaClip(albedoAlpha.a);

                Light mainLight = GetEffectiveMainLight(input.positionWS);
                IndirectLighting indirect = SampleIndirectLighting(input.positionWS, normalWS, viewDirWS, input.positionCS);

                half3 ambient = lerp(indirect.diffuse, _AmbientColor.rgb, _AmbientColor.a);
                half3 toonLighting = CalculateToonLighting(normalWS, input.positionWS, mainLight);

                half3 color = albedoAlpha.rgb * (toonLighting + ambient) + indirect.specular;
                color = ApplyEmission(color, input.uv);

                return half4(color, albedoAlpha.a);
            }
            ENDHLSL
        }

            // ==================== SHADOWCASTER (fix GetShadowCasterPositionCS) ====================
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On ZTest LEqual ColorMask 0 Cull [_CullMode]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local_fragment _ALPHACLIP_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Includes/Toon/ToonLitCore.hlsl"

            struct VaryingsShadow
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            VaryingsShadow ShadowVert(Attributes input)
            {
                VaryingsShadow output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                    // Hàm đúng trong URP mới nhất
                output.positionCS = TransformWorldToHClipApplyShadowBias(positionWS, normalWS, _ShadowBias);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ShadowFrag(VaryingsShadow input) : SV_Target
            {
                ApplyAlphaClip(GetAlbedoAndAlpha(input.uv).a);
                return 0;
            }
            ENDHLSL
        }

            // ==================== DEPTH ONLY ====================
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
            ZWrite On ColorMask 0 Cull [_CullMode]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Includes/Toon/ToonLitCore.hlsl"

            float4 Vert(Attributes input) : SV_POSITION
            {
                return TransformObjectToHClip(input.positionOS.xyz);
            }
            half4 Frag(float2 uv : TEXCOORD0) : SV_Target
            {
                ApplyAlphaClip(GetAlbedoAndAlpha(uv).a);
                return 0;
            }
            ENDHLSL
        }

            // ==================== DEPTH NORMALS ====================
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }
            ZWrite On Cull [_CullMode]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _NORMALMAP_ON
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Includes/Toon/ToonLitCore.hlsl"

            struct VaryingsDN
            {
                float4 posCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                #ifdef _NORMALMAP_ON
                    float4 tangentWS : TEXCOORD2;
                #endif
            };

            VaryingsDN Vert(Attributes input)
            {
                VaryingsDN o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                o.posCS = p.positionCS;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.normalWS = n.normalWS;
                #ifdef _NORMALMAP_ON
                    o.tangentWS = float4(n.tangentWS, n.bitangentWS.x);
                #endif
                return o;
            }

            half4 Frag(VaryingsDN i, half facing : VFACE) : SV_Target
            {
                ApplyAlphaClip(GetAlbedoAndAlpha(i.uv).a);
                float3 normalWS = normalize(i.normalWS) * (facing > 0 ? 1 : -1);
                #ifdef _NORMALMAP_ON
                    float3 tangent = i.tangentWS.xyz;
                    float3 bitangent = cross(i.normalWS, tangent) * i.tangentWS.w;
                    normalWS = ApplyNormalMap(i.uv, normalWS, tangent, bitangent);
                #endif
                return half4(PackNormalOctRectEncode(TransformWorldToViewNormal(normalWS)), 0.0, 0.0);
            }
            ENDHLSL
        }

            // ==================== DEFERRED GBUFFER ====================
        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }

            ZWrite [_ZWrite] Cull [_CullMode] Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _NORMALMAP_ON
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Includes/Toon/ToonLitCore.hlsl"

            struct VaryingsG
            {
                float4 posCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                #ifdef _NORMALMAP_ON
                    float4 tangentWS : TEXCOORD2;
                #endif
            };

            VaryingsG Vert(Attributes input)
            {
                VaryingsG o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                o.posCS = p.positionCS;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.normalWS = n.normalWS;
                #ifdef _NORMALMAP_ON
                    o.tangentWS = float4(n.tangentWS, n.bitangentWS.x);
                #endif
                return o;
            }

            void Frag(VaryingsG i,
            out half4 GBuffer0 : SV_Target0,
            out half4 GBuffer1 : SV_Target1,
            out half4 GBuffer2 : SV_Target2,
            out half4 GBuffer3 : SV_Target3,
            half facing : VFACE)
            {
                half4 albedo = GetAlbedoAndAlpha(i.uv);
                ApplyAlphaClip(albedo.a);

                float3 normalWS = normalize(i.normalWS) * (facing > 0 ? 1 : -1);
                #ifdef _NORMALMAP_ON
                    float3 tangent = i.tangentWS.xyz;
                    float3 bitangent = cross(i.normalWS, tangent) * i.tangentWS.w;
                    normalWS = ApplyNormalMap(i.uv, normalWS, tangent, bitangent);
                #endif

                half3 emission = 0;
                #ifdef _EMISSION_ON
                    emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
                    #

                    GBuffer0 = half4(albedo.rgb, 0);
                    GBuffer1 = half4(0, 0, 0, 0);
                    GBuffer2 = half4(normalWS * 0.5h + 0.5h, 0);
                    GBuffer3 = half4(emission, 1);
                }
                ENDHLSL
            }

                // ==================== META ====================
            Pass
            {
                Name "Meta"
                Tags
                {
                    "LightMode" = "Meta"
                }
                Cull Off

                HLSLPROGRAM
                #pragma vertex MetaVert
                #pragma fragment MetaFrag
                #pragma shader_feature_local_fragment _EMISSION_ON

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
                #include "Includes/Toon/ToonLitCore.hlsl"

                struct MetaInput
                {
                    float4 positionOS : POSITION;
                    float2 uv : TEXCOORD0;
                    float2 uvLM : TEXCOORD1;
                };

                struct MetaVaryings
                {
                    float4 posCS : SV_POSITION;
                    float2 uv : TEXCOORD0;
                };

                MetaVaryings MetaVert(MetaInput input)
                {
                    MetaVaryings o;
                    o.posCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uvLM, input.uv);
                    o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                    return o;
                }

                half4 MetaFrag(MetaVaryings i) : SV_Target
                {
                    UnityMetaInput meta;
                    ZERO_INITIALIZE(UnityMetaInput, meta);

                    half4 albedo = GetAlbedoAndAlpha(i.uv);
                    meta.Albedo = albedo.rgb;
                    meta.Emission = ApplyEmission(0, i.uv);

                    return UnityMetaFragment(meta);
                }
                ENDHLSL
            }
        }

        CustomEditor "ToonLitShaderGUI"
    }
