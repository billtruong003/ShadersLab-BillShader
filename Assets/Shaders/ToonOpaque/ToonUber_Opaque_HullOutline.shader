Shader "Bill's Toon/Opaque (Hull Outline)"
{
    Properties
    {
        [HideInInspector] _SurfaceType("Surface Type", Float) = 0

        [Header(Render States)]
        [Enum(Opaque, 0, Cutout, 1, Transparent, 2)] _RenderMode ("Render Mode", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 10
        [Toggle] _ZWrite ("ZWrite", Float) = 1

        [Header(Outline Properties (Inverted Hull))]
        _OutlineColor("Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Width", Range(0.0, 10)) = 1.0
        [Toggle(_OUTLINE_SCALE_WITH_DISTANCE)] _OutlineScaleWithDistance("Screen-Space Scaling", Float) = 1
        _OutlineDistanceFadeStart("Distance Fade Start", Float) = 20
        _OutlineDistanceFadeEnd("Distance Fade End", Float) = 30

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

        [Header(Rim Light)]
        [Toggle(_RIMLIGHT_ON)] _RimLightToggle("Enable Rim Light", Float) = 0
        [HDR] _RimColor("Rim Color", Color) = (1, 0.3, 0.3, 1)
        _RimPower("Rim Power", Range(0, 20)) = 6

        [Header(Dynamic Texture Mask)]
        [Toggle(_TEXTUREMASK_ON)] _TextureMaskToggle("Enable Texture Mask", Float) = 0
        [NoScaleOffset] _MaskTexture("Mask Texture (Grayscale)", 2D) = "gray"{}
        [IntRange] _MaskDivisions("Mask Divisions (e.g., 2=2x2)", Range(1, 4)) = 2
        _MaskBlend("Mask Blend Strength", Range(0, 1)) = 1.0
        [HideInInspector] _MaskColor0("Mask Color 0", Color) = (1, 0, 0, 1)
        [HideInInspector] _MaskColor1("Mask Color 1", Color) = (0, 1, 0, 1)
        [HideInInspector] _MaskColor2("Mask Color 2", Color) = (0, 0, 1, 1)
        [HideInInspector] _MaskColor3("Mask Color 3", Color) = (1, 1, 0, 1)
        [HideInInspector] _MaskColor4("Mask Color 4", Color) = (1, 1, 1, 1)
        [HideInInspector] _MaskColor5("Mask Color 5", Color) = (1, 1, 1, 1)
        [HideInInspector] _MaskColor6("Mask Color 6", Color) = (1, 1, 1, 1)
        [HideInInspector] _MaskColor7("Mask Color 7", Color) = (1, 1, 1, 1)
        [HideInInspector] _MaskColor8("Mask Color 8", Color) = (1, 1, 1, 1)
        [HideInInspector] _MaskColor9("Mask Color 9", Color) = (1, 1, 1, 1)
        [HideInInspector] _MaskColor10("Mask Color 10", Color) = (1, 1, 1, 1)
        [HideInInspector] _MaskColor11("Mask Color 11", Color) = (1, 1, 1, 1)
        [HideInInspector] _MaskColor12("Mask Color 12", Color) = (1, 1, 1, 1)
        [HideInInspector] _MaskColor13("Mask Color 13", Color) = (1, 1, 1, 1)
        [HideInInspector] _MaskColor14("Mask Color 14", Color) = (1, 1, 1, 1)
        [HideInInspector] _MaskColor15("Mask Color 15", Color) = (1, 1, 1, 1)

        [Header(Advanced Dither Fade)]
        [Toggle(_DITHERFADE_ON)] _DitherFadeToggle("Enable Dither Fade", Float) = 0
        _DitherPatternTex("Dither Pattern (Bayer/Blue Noise)", 2D) = "white"{}
        _DitherScale("Dither Pattern Scale", Range(1.0, 200.0)) = 50.0
        _DitherFadeStart("Dither Fade Start (Far)", Float) = 2.0
        _DitherFadeEnd("Dither Fade End (Near)", Float) = 1.0
        [HDR] _DitherEdgeColor("Dither Edge Color", Color) = (0.5, 0.8, 1.0, 1.0)
        _DitherEdgeWidth("Dither Edge Width", Range(0.01, 0.5)) = 0.1

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

        [Header(Foliage)]
        [NoScaleOffset] _WindNoiseTex("Wind Noise (Seamless, Grayscale)", 2D) = "gray"{}
        _WindSpeed("Wind Speed", Range(0, 10)) = 2.0
        _WindAmplitude("Wind Amplitude", Range(0, 1)) = 0.1
        _WindNoiseScale("Wind Noise Scale", Range(0.1, 10)) = 1.0
        _WindDirection("Wind Direction", Vector) = (1, 0, 0.5, 0)
        _WindFadeStart("Wind Fade Start", Float) = 20.0
        _WindFadeEnd("Wind Fade End", Float) = 50.0
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
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry"
        }

        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag

            #pragma shader_feature_local _OUTLINE_SCALE_WITH_DISTANCE
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE

            #include "Includes/Toon/ToonUberCore.hlsl"

            struct OutlineVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            OutlineVaryings OutlineVert(Attributes input)
            {
                OutlineVaryings o;
                float3 positionOS = input.positionOS.xyz;
                float3 normalOS = input.normalOS;

                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(positionOS, input.color);
                #endif

                float3 positionWS = TransformObjectToWorld(positionOS);
                float camDist = distance(positionWS, _WorldSpaceCameraPos.xyz);

                    // Scale outline based on distance to camera to maintain visibility
                float distFade = 1.0 - saturate((camDist - _OutlineDistanceFadeStart) / max(0.001, _OutlineDistanceFadeEnd - _OutlineDistanceFadeStart));
                float scaledWidth = _OutlineWidth * 0.01 * distFade;

                #if defined(_OUTLINE_SCALE_WITH_DISTANCE)
                        // Screen-space scaling logic
                    o.positionCS = TransformObjectToHClip(positionOS);
                    float3 normalWS = TransformObjectToWorldNormal(normalOS);
                    float3 normalVS = TransformWorldToViewDir(normalWS);
                    float2 screenSpaceNormal = normalize(mul((float2x3)UNITY_MATRIX_P, normalVS).xy);
                    o.positionCS.xy += screenSpaceNormal * scaledWidth * o.positionCS.w;
                #else
                            // Object-space scaling logic
                    positionOS += normalOS * scaledWidth;
                    o.positionCS = TransformObjectToHClip(positionOS);
                #endif

                return o;
            }

            half4 OutlineFrag(OutlineVaryings i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            Cull Back
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
            #pragma shader_feature_local_fragment _BLING_WORLDSPACE_ON
            #pragma shader_feature_local_fragment _INDIRECTSPECULAR_ON
            #pragma shader_feature_local_fragment _TOON_STYLE_HARD
            #pragma shader_feature_local_fragment _MORPH_ON
            #pragma shader_feature_local_fragment _TEXTUREMASK_ON
                // Added Rim Light Support for Body
            #pragma shader_feature_local_fragment _RIMLIGHT_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ PROBE_VOLUMES_L1
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES

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
                    lighting = CalculateToonLighting(normalWS, i.positionWS, mainLight, viewDir);
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

                surfaceColor = ApplyTextureMask(surfaceColor, i.uv);
                surfaceColor += indirectLighting.specular;
                surfaceColor = ApplyEmission(surfaceColor, i.uv);
                surfaceColor += ditherEdgeColor;

                return half4(surfaceColor, albedo.a);
            }
            ENDHLSL
        }

            // --- ShadowCaster Pass ---
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            ZWrite On ZTest LEqual Cull Back ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE
            #pragma shader_feature_local_fragment _MORPH_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Includes/Toon/ToonUberCore.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Varyings_Shadows
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings_Shadows ShadowVert(Attributes input)
            {
                Varyings_Shadows output;
                float3 positionOS = input.positionOS.xyz;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(positionOS, input.color);
                #endif
                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_MainLightPosition.xyz - positionWS);
                #else
                        float3 lightDirectionWS = _MainLightPosition.xyz;
                #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ShadowFrag(Varyings_Shadows input) : SV_Target
            {
                #if defined(_ALPHACLIP_ON)
                    ApplyAlphaClip(input.uv);
                #endif
                return 0;
            }
            ENDHLSL
        }

            // --- DepthOnly Pass ---
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

            struct Varyings_Depth
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings_Depth DepthVert(Attributes input)
            {
                Varyings_Depth output;
                float3 posOS = input.positionOS.xyz;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(posOS, input.color);
                #endif
                output.positionCS = TransformObjectToHClip(posOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 DepthFrag(Varyings_Depth i) : SV_Target
            {
                ApplyAlphaClip(i.uv);
                return 0;
            }
            ENDHLSL
        }

            // --- DepthNormals Pass ---
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
                float3 normalWS : TEXCOORD1;
                #if defined(_NORMALMAP_ON)
                    float4 tangentWS : TEXCOORD2;
                #endif
            };

            DepthNormalsVaryings DepthNormalsVert(Attributes input)
            {
                DepthNormalsVaryings o;
                float3 posOS = input.positionOS.xyz;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(posOS, input.color);
                #endif

                o.positionCS = TransformObjectToHClip(posOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_NORMALMAP_ON)
                    float3 tWS = TransformObjectToWorldDir(input.tangentOS.xyz);
                    o.tangentWS = float4(tWS.x, tWS.y, tWS.z, input.tangentOS.w);
                #endif
                return o;
            }

            half4 DepthNormalsFrag(DepthNormalsVaryings i, half facing : VFACE) : SV_Target
            {
                ApplyAlphaClip(i.uv);
                float3 normalWS = normalize(i.normalWS) * (facing * 2 - 1);
                #if defined(_NORMALMAP_ON)
                    float3 bitangentWS = cross(i.normalWS, i.tangentWS.xyz) * i.tangentWS.w;
                    normalWS = ApplyNormalMap(i.uv, normalWS, i.tangentWS.xyz, bitangentWS);
                #endif

                float3 viewNormal = TransformWorldToViewNormal(normalWS);
                return half4(PackNormalOctRectEncode(viewNormal), 0, 0);
            }
            ENDHLSL
        }

            // --- MotionVectors Pass ---
        Pass
        {
            Name "MotionVectors"
            Tags
            {
                "LightMode" = "MotionVectors"
            }
            ZWrite Off Cull [_CullMode] ColorMask RG

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _ALPHACLIP_ON
            #pragma shader_feature_local _SURFACETYPE_FOLIAGE
            #pragma shader_feature_local_fragment _MORPH_ON
            #include "Includes/Toon/ToonUberCore.hlsl"

            struct VaryingsMotion
            {
                float4 positionCS : SV_POSITION;
                float4 positionCS_NoJitter : TEXCOORD0;
                float4 previousPositionCS_NoJitter : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            VaryingsMotion Vert(Attributes input)
            {
                VaryingsMotion o;
                float3 posOS = input.positionOS.xyz;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(posOS, input.color);
                #endif

                o.positionCS = TransformObjectToHClip(posOS);
                o.positionCS_NoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, float4(posOS, 1.0)));

                float4 prevPosOS = float4(posOS, 1.0);
                float4 prevPosWS = mul(unity_MatrixPreviousM, prevPosOS);
                o.previousPositionCS_NoJitter = mul(unity_MatrixPreviousVP, prevPosWS);

                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 Frag(VaryingsMotion i) : SV_Target
            {
                ApplyAlphaClip(i.uv);
                float2 curPos = i.positionCS_NoJitter.xy / i.positionCS_NoJitter.w;
                float2 prevPos = i.previousPositionCS_NoJitter.xy / i.previousPositionCS_NoJitter.w;
                return float4(curPos - prevPos, 0, 0);
            }
            ENDHLSL
        }

            // --- GBuffer Pass ---
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
            #include "Includes/Toon/ToonUberCore.hlsl"

            struct GBufferVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                #if defined(_NORMALMAP_ON)
                    float4 tangentWS : TEXCOORD3;
                #endif
            };

            struct ManualGBufferOutput
            {
                half4 GBuffer0 : SV_Target0;
                half4 GBuffer1 : SV_Target1;
                half4 GBuffer2 : SV_Target2;
                half4 GBuffer3 : SV_Target3;
            };

            GBufferVaryings GBufferVert(Attributes input)
            {
                GBufferVaryings o;
                float3 posOS = input.positionOS.xyz;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(posOS, input.color);
                #endif

                o.positionWS = TransformObjectToWorld(posOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                #if defined(_NORMALMAP_ON)
                    float3 tWS = TransformObjectToWorldDir(input.tangentOS.xyz);
                    o.tangentWS = float4(tWS.x, tWS.y, tWS.z, input.tangentOS.w);
                #endif
                return o;
            }

            ManualGBufferOutput GBufferFrag(GBufferVaryings i, half facing : VFACE)
            {
                ApplyAlphaClip(i.uv);

                float3 normalWS = normalize(i.normalWS) * (facing * 2.0 - 1.0);
                #if defined(_NORMALMAP_ON)
                    float3 bitangentWS = cross(i.normalWS, i.tangentWS.xyz) * i.tangentWS.w;
                    normalWS = ApplyNormalMap(i.uv, normalWS, i.tangentWS.xyz, bitangentWS);
                #endif

                half4 albedo = GetAlbedoAndAlpha(i.uv);
                half3 emission = ApplyEmission(0, i.uv);

                ManualGBufferOutput output;
                output.GBuffer0 = half4(albedo.rgb * _BaseColor.rgb, 0);
                output.GBuffer1 = half4(0, 0, 0, 0);
                output.GBuffer2 = half4(normalWS * 0.5 + 0.5, 0);
                output.GBuffer3 = half4(emission, 1);
                return output;
            }
            ENDHLSL
        }

            // --- Meta Pass ---
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
                o.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1, input.uv1, unity_LightmapST, unity_DynamicLightmapST);
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

            // --- Scene Selection ---
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
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Includes/Toon/ToonUberCore.hlsl"

            float4 Vert(Attributes input) : SV_POSITION
            {
                float3 posOS = input.positionOS.xyz;
                #if defined(_SURFACETYPE_FOLIAGE)
                    ApplyWind(posOS, input.color);
                #endif
                return TransformObjectToHClip(posOS);
            }

            half4 Frag() : SV_Target
            {
                return half4(1, 1, 1, 1);
            }
            ENDHLSL
        }
    }
    CustomEditor "ToonOpaqueHullOutlineShaderGUI"
}
