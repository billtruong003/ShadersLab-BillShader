Shader "Bill's Toon/Opaque - Full URP Compatible"
{
    Properties
    {
        [HideInInspector] _SurfaceType("Surface Type", Float) = 0

        [Header(Render States)]
        [Enum(Opaque, 0, Cutout, 1, Transparent, 2)] _RenderMode ("Render Mode", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 10
        [Toggle] _ZWrite ("ZWrite", Float) = 1

        [Header(Base Properties)]
        _BaseMap("Albedo A (RGB) Alpha (A)", 2D) = "white"{}
        [HDR] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump"{}
        _BumpScale("Normal Intensity", Range(0.0, 2.0)) = 1.0

        [Header(Texture Morph)]
        [Toggle(_MORPH_ON)] _MorphToggle("Enable Morph", Float) = 0
        [NoScaleOffset] _BaseMapB("Albedo B (RGB) Alpha (A)", 2D) = "white"{}
        _Morph("Morph (0=A, 1=B)", Range(0, 1)) = 0

        [Header(Alpha Clipping)]
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Header(Emission)]
        [Toggle(_EMISSION_ON)] _EmissionMode("Enable Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionMap("Emission Map", 2D) = "black"{}

        [Header(Advanced Dither Fade)]
        [Toggle(_DITHERFADE_ON)] _DitherFadeToggle("Enable Dither Fade", Float) = 0
        _DitherPatternTex("Dither Pattern (Bayer/Blue Noise)", 2D) = "white"{}
        _DitherScale("Dither Pattern Scale", Range(1.0, 200.0)) = 50.0
        _DitherFadeStart("Dither Fade Start (Far)", Float) = 2.0
        _DitherFadeEnd("Dither Fade End (Near)", Float) = 1.0
        [HDR] _DitherEdgeColor("Dither Edge Color", Color) = (0.5, 0.8, 1.0, 1.0)
        _DitherEdgeWidth("Dither Edge Width", Range(0.01, 0.5)) = 0.1

        [Header(Render States Advanced)]
        [Enum(Off, 0, Front, 1, Back, 2)] _CullMode ("Culling Mode", Float) = 2

        [Header(Lighting)]
        [Toggle(_FORCE_FAKELIGHT_ON)] _ForceFakeLight("Force Fake Light", Float) = 0
        [Toggle(_FAKELIGHT_ON)] _FakeLightMode("Enable Fake Light Fallback", Float) = 1
        _FakeLightColor("Fake Light Color", Color) = (0.8, 0.8, 0.8, 1)
        _FakeLightDirection("Fake Light Direction", Vector) = (0.5, 0.5, -0.5, 0)
        _AmbientColor("Ambient Color", Color) = (0.5, 0.5, 0.5, 0)
        _MaxBrightness("Max Brightness", Range(0, 5)) = 1.5

        [Header(Indirect Lighting)]
        [Toggle(_INDIRECTSPECULAR_ON)] _IndirectSpecular("Enable Environment Reflections", Float) = 0
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

        [Header(Stylized Metal)]
        _Ramp("Toon Ramp (RGB)", 2D) = "white"{}
        _Brightness("Specular Brightness", Range(0, 2)) = 1.3
        _Offset("Specular Size", Range(0, 1)) = 0.8
        [HDR] _SpecuColor("Specular Color", Color) = (0.8, 0.45, 0.2, 1)
        _HighlightOffset("Highlight Size", Range(0, 1)) = 0.9
        [HDR] _HiColor("Highlight Color", Color) = (1, 1, 1, 1)
        [HDR] _RimColor("Rim Color", Color) = (1, 0.3, 0.3, 1)
        _RimPower("Rim Power", Range(0, 20)) = 6

        [Header(Foliage)]
        _WindFrequency("Wind Frequency", Range(0.1, 10)) = 2.0
        _WindAmplitude("Wind Amplitude", Range(0, 1)) = 0.1
        _WindDirection("Wind Direction", Vector) = (1, 0, 0.5, 0)
        [HDR] _TranslucencyColor("Translucency Color", Color) = (0.7, 0.9, 0.3, 1)
        _TranslucencyStrength("Translucency Strength", Range(0, 5)) = 1.0

        [Header(Bling Effect)]
        [NoScaleOffset] _NoiseTex("Noise Texture (Grayscale, Tiling)", 2D) = "gray"{}
        [Toggle(_BLING_WORLDSPACE_ON)] _BlingWorldSpace("Use World Space Bling", Float) = 0
        [HDR] _BlingColor("Bling Color", Color) = (1, 1, 1, 1)
        _BlingIntensity("Bling Intensity", Range(0, 10)) = 2.0
        _BlingScale("Bling Scale", Range(1, 10000)) = 50.0
        _BlingSpeed("Bling Speed", Range(0, 5)) = 1.0
        _BlingFresnelPower("Bling Fresnel Power", Range(0.1, 10)) = 2.0
        _BlingThreshold("Bling Threshold", Range(0.5, 1.0)) = 0.95

        [Header(Outline Properties (Fresnel))]
        [Toggle(_OUTLINEMODE_FRESNEL)] _FresnelOutlineToggle("Enable Fresnel Outline", Float) = 1
        [HDR] _FresnelOutlineColor("Color", Color) = (0, 0, 0, 1)
        _FresnelOutlineWidth("Width", Range(0.001, 1.0)) = 0.1
        _FresnelOutlinePower("Power", Range(1.0, 100.0)) = 5.0
        _FresnelOutlineSharpness("Sharpness", Range(0.1, 10.0)) = 2.0

        [Toggle(_OUTLINEGLINT_ON)] _GlintToggle("Enable Glint Effect", Float) = 0
        [HDR] _GlintColor("Glint Color", Color) = (1, 1, 0.5, 1)
        _GlintScale("Glint Scale", Float) = 20.0
        _GlintSpeed("Glint Speed", Range(0.1, 10.0)) = 2.0
        _GlintThreshold("Glint Threshold", Range(0.5, 0.99)) = 0.95
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

            // ===================================================================
            // 1. Main Forward Pass
            // ===================================================================
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
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local _SURFACETYPE_OPAQUE _SURFACETYPE_METALLIC _SURFACETYPE_FOLIAGE _SURFACETYPE_BLING
            #pragma shader_feature_local_fragment _NORMALMAP_ON
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _FORCE_FAKELIGHT_ON
            #pragma shader_feature_local_fragment _FAKELIGHT_ON
            #pragma shader_feature_local_fragment _DITHERFADE_ON
            #pragma shader_feature_local_fragment _OUTLINEMODE_FRESNEL
            #pragma shader_feature_local_fragment _OUTLINEGLINT_ON
            #pragma shader_feature_local_fragment _BLING_WORLDSPACE_ON
            #pragma shader_feature_local_fragment _INDIRECTSPECULAR_ON
            #pragma shader_feature_local_fragment _TOON_STYLE_HARD
            #pragma shader_feature_local_fragment _MORPH_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ PROBE_VOLUMES_L1

            #include "Includes/Toon/ToonUberCore.hlsl"

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                float3 positionOS = v.positionOS.xyz;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(positionOS, v.color);
                #endif
                o.positionWS = TransformObjectToWorld(positionOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color;
                o.screenPos = ComputeScreenPos(o.positionCS);

                #if defined(_NORMALMAP_ON)
                    o.tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                    o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangentOS.w;
                #endif

                return o;
            }

            half4 frag(Varyings i, half frontFace : VFACE) : SV_Target
            {
                float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - i.positionWS);

                float3 baseNormalWS = normalize(i.normalWS * sign(frontFace));
                float3 normalWS = baseNormalWS;
                #if defined(_NORMALMAP_ON)
                    normalWS = ApplyNormalMap(i.uv, baseNormalWS, i.tangentWS, i.bitangentWS);
                #endif

                half3 ditherEdgeColor = ApplyDitherFade(i.screenPos);
                ApplyAlphaClip(i.uv);

                half4 albedo = GetAlbedoAndAlpha(i.uv);
                Light mainLight = GetEffectiveMainLight(i.positionWS);

                IndirectLighting indirectLighting = SampleIndirectLighting(i.positionWS, normalWS, viewDir, i.positionCS);
                half3 ambient = lerp(indirectLighting.diffuse, _AmbientColor.rgb, _AmbientColor.a);

                half3 lighting = 0;
                #if defined(_SURFACETYPE_OPAQUE)
                    lighting = CalculateToonLighting(normalWS, i.positionWS, mainLight);
                #elif defined(_SURFACETYPE_METALLIC)
                    lighting = CalculateMetallicLighting(normalWS, viewDir, mainLight);
                #elif defined(_SURFACETYPE_FOLIAGE)
                    lighting = CalculateFoliageLighting(normalWS, i.positionWS, mainLight);
                #elif defined(_SURFACETYPE_BLING)
                    lighting = CalculateBlingLighting(albedo.rgb, normalWS, i.positionWS, mainLight, viewDir, i.screenPos);
                #endif

                half3 surfaceColor;
                #if defined(_SURFACETYPE_BLING)
                    surfaceColor = lighting + ambient;
                #else
                        surfaceColor = albedo.rgb * (lighting + ambient);
                #endif

                surfaceColor += indirectLighting.specular;
                surfaceColor = ApplyEmission(surfaceColor, i.uv);
                surfaceColor = ApplyFresnelOutline(surfaceColor, normalWS, viewDir, i.positionWS);
                surfaceColor += ditherEdgeColor;

                return half4(surfaceColor, albedo.a);
            }
            ENDHLSL
        }
            // ===================================================================
            // SHADOW CASTER PASS – FIXED 2025 (Cast shadow chính xác)
            // ===================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On ZTest LEqual Cull [_CullMode] ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE
            #pragma shader_feature_local_fragment _MORPH_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Includes/Toon/ToonUberCore.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = input.positionOS.xyz;
                float3 normalOS = input.normalOS;

                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(positionOS, input.color);
                #endif

                VertexPositionInputs vertexPosition = GetVertexPositionInputs(positionOS);
                VertexNormalInputs vertexNormal = GetVertexNormalInputs(normalOS);

                    // ===== BIAS CHUẨN URP 2024-2025 =====
                float3 positionWS = vertexPosition.positionWS;
                float3 normalWS = vertexNormal.normalWS;

                float4 clipPos;
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirWS = normalize(_LightPosition - positionWS);
                #else
                        float3 lightDirWS = _LightDirection;
                #endif

                    // Apply shadow bias
                float depthBias = _ShadowBias.x;
                float normalBias = _ShadowBias.y * saturate(dot(normalWS, lightDirWS));
                positionWS += (normalWS * normalBias + lightDirWS * depthBias);

                clipPos = TransformWorldToHClip(positionWS);

                #if UNITY_REVERSED_Z
                    clipPos.z = min(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
                #else
                        clipPos.z = max(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = clipPos;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                #if defined(_ALPHACLIP_ON)
                    ApplyAlphaClip(input.uv);
                #endif
                return 0;
            }
            ENDHLSL
        }

            // ===================================================================
            // 3. Depth Only
            // ===================================================================
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On ColorMask 0 Cull [_CullMode]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE
            #pragma shader_feature_local_fragment _MORPH_ON
            #include "Includes/Toon/ToonUberCore.hlsl"

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            DepthVaryings DepthVert(Attributes input)
            {
                DepthVaryings o;
                float3 posOS = input.positionOS.xyz;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(posOS, input.color);
                #endif
                VertexPositionInputs vertexInput = GetVertexPositionInputs(posOS);
                o.positionCS = vertexInput.positionCS;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 DepthFrag(DepthVaryings i) : SV_Target
            {
                ApplyAlphaClip(i.uv);
                return 0;
            }
            ENDHLSL
        }

            // ===================================================================
            // 4. Depth Normals
            // ===================================================================
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            ZWrite On Cull [_CullMode]

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma shader_feature_local_fragment _NORMALMAP_ON
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE
            #pragma shader_feature_local_fragment _MORPH_ON
            #include "Includes/Toon/ToonUberCore.hlsl"

            struct DepthNormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 normalWS : TEXCOORD1;
                #if defined(_NORMALMAP_ON)
                    float4 tangentWS : TEXCOORD2;
                    float4 bitangentWS : TEXCOORD3;
                #endif
            };

            DepthNormalsVaryings DepthNormalsVert(Attributes input)
            {
                DepthNormalsVaryings o;
                float3 posOS = input.positionOS.xyz;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(posOS, input.color);
                #endif

                VertexPositionInputs posInput = GetVertexPositionInputs(posOS);
                VertexNormalInputs normInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                o.positionCS = posInput.positionCS;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.normalWS = float4(normInput.normalWS, 0);

                #if defined(_NORMALMAP_ON)
                    o.tangentWS = float4(normInput.tangentWS, 0);
                    o.bitangentWS = float4(normInput.bitangentWS, 0);
                #endif
                return o;
            }

            half4 DepthNormalsFrag(DepthNormalsVaryings i, half facing : VFACE) : SV_Target
            {
                ApplyAlphaClip(i.uv);
                float3 normalWS = normalize(i.normalWS.xyz) * sign(facing);
                #if defined(_NORMALMAP_ON)
                    normalWS = ApplyNormalMap(i.uv, normalWS, i.tangentWS.xyz, i.bitangentWS.xyz);
                #endif
                return half4(PackNormalOctRectEncode(TransformWorldToViewNormal(normalWS)), 0, 0);
            }
            ENDHLSL
        }

            // ===================================================================
            // Motion Vectors – Bản fix 2025 (không dùng MotionVectorsCore nữa)
            // ===================================================================
        Pass
        {
            Name "MotionVectors"
            Tags
            {
                "LightMode" = "MotionVectors"
            }

            ZWrite Off Cull [_CullMode]
            ColorMask RG

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE
            #pragma shader_feature_local_fragment _MORPH_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectors.hlsl"
            #include "Includes/Toon/ToonUberCore.hlsl"

            VaryingsMotion Vert(Attributes input)
            {
                VaryingsMotion o;
                UNITY_INITIALIZE_OUTPUT(VaryingsMotion, o);

                float3 posOS = input.positionOS.xyz;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(posOS, input.color);
                #endif

                VertexPositionInputs pos = GetVertexPositionInputs(posOS);
                o.positionCS = pos.positionCS;
                o.previousPositionCS = GetPreviousPositionCS(posOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return o;
            }

            half4 Frag(VaryingsMotion i) : SV_Target
            {
                ApplyAlphaClip(i.uv);
                return MotionVectorFragment(i.positionCS, i.previousPositionCS);
            }
            ENDHLSL
        }

            // ===================================================================
            // 6. GBuffer (Deferred Rendering)
            // ===================================================================
        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }

            ZWrite [_ZWrite]
            Cull [_CullMode]
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma vertex GBufferVert
            #pragma fragment GBufferFrag
            #pragma shader_feature_local_fragment _NORMALMAP_ON
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE
            #pragma shader_feature_local_fragment _MORPH_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Includes/Toon/ToonUberCore.hlsl"

            struct GBufferVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                #if defined(_NORMALMAP_ON)
                    float4 tangentWS : TEXCOORD3;
                    float4 bitangentWS : TEXCOORD4;
                #endif
            };

            GBufferVaryings GBufferVert(Attributes input)
            {
                GBufferVaryings o;
                float3 posOS = input.positionOS.xyz;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(posOS, input.color);
                #endif

                VertexPositionInputs posInput = GetVertexPositionInputs(posOS);
                VertexNormalInputs normInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                o.positionCS = posInput.positionCS;
                o.positionWS = posInput.positionWS;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.normalWS = normInput.normalWS;

                #if defined(_NORMALMAP_ON)
                    o.tangentWS.xyz = normInput.tangentWS;
                    o.bitangentWS.xyz = normInput.bitangentWS;
                #endif
                return o;
            }

            GBufferOutput GBufferFrag(GBufferVaryings i, half facing : VFACE)
            {
                ApplyAlphaClip(i.uv);

                float3 normalWS = normalize(i.normalWS) * sign(facing);
                #if defined(_NORMALMAP_ON)
                    normalWS = ApplyNormalMap(i.uv, normalWS, i.tangentWS.xyz, i.bitangentWS.xyz);
                #endif

                half4 albedo = GetAlbedoAndAlpha(i.uv);
                half3 emission = ApplyEmission(0, i.uv);

                GBufferOutput o;
                o.GBuffer0 = half4(albedo.rgb * _BaseColor.rgb, 0);
                o.GBuffer1 = half4(0, 0, 0, 0);
                o.GBuffer2 = half4(normalWS * 0.5 + 0.5, 0);
                o.GBuffer3 = half4(emission, 1);
                return o;
            }
            ENDHLSL
        }

            // ===================================================================
            // 7. Meta (Lightmap & Light Probe Baking)
            // ===================================================================
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
            #pragma shader_feature_local_fragment _MORPH_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
            #include "Includes/Toon/ToonUberCore.hlsl"

            struct MetaVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            MetaVaryings MetaVert(Attributes input)
            {
                MetaVaryings o;
                o.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv0, input.uv1);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 MetaFrag(MetaVaryings i) : SV_Target
            {
                UnityMetaInput meta;
                UNITY_INITIALIZE_OUTPUT(UnityMetaInput, meta);

                half4 albedo = GetAlbedoAndAlpha(i.uv);
                meta.Albedo = albedo.rgb * _BaseColor.rgb;
                meta.Emission = ApplyEmission(0, i.uv);

                return UnityMetaFragment(meta);
            }
            ENDHLSL
        }

            // ===================================================================
            // SceneSelection + ScenePicking – Bản inline, không cần file riêng
            // ===================================================================
        Pass
        {
            Name "SceneSelectionPass"
            Tags
            {
                "LightMode" = "SceneSelectionPass"
            }

            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Version.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Includes/Toon/ToonUberCore.hlsl"

            struct AttributesLean
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vert(AttributesLean input) : SV_POSITION
            {
                float3 posOS = input.positionOS.xyz;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(posOS, input.color);
                #endif
                return TransformObjectToHClip(posOS);
            }

            half4 Frag() : SV_Target
            {
                return half4(1, 1, 1, 1); // Unity sẽ tự override màu highlight
            }
            ENDHLSL
        }
    }

    CustomEditor "ToonOpaqueShaderGUI"
}
